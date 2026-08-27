using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using RonSijm.AnaalIJzer.Core.Observations;
using RonSijm.AnaalIJzer.Core.SourceLocations;
using RonSijm.AnaalIJzer.Diagnostics;
using AnalyzerConfig = RonSijm.AnaalIJzer.Core.RuntimeConfig.Config.Model.AnalyzerConfig;

namespace RonSijm.AnaalIJzer.Engine.Analysis.Placement.SourceLocations;

internal static class LayerSourceLocationAnalyzer
{
	internal static void AnalyzeSymbol(SymbolAnalysisContext context, AnalyzerConfig config, AnalyzerBuildProperties buildProperties, ConcurrentDictionary<ISymbol, byte> analyzedSymbols)
	{
		if (context.Symbol is not INamedTypeSymbol typeSymbol
		    || typeSymbol.IsImplicitlyDeclared
		    || !analyzedSymbols.TryAdd(typeSymbol, 0))
		{
			return;
		}

		var layerMatch = config.Engine.FindLayer(typeSymbol.Name, typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty, typeSymbol);
		if (layerMatch is null || layerMatch.Value.Layer.IsForbidden)
		{
			return;
		}

		var policies = config.Engine.GetSourceLocationPolicies(layerMatch.Value);
		if (policies.IsDefaultOrEmpty)
		{
			return;
		}

		var assemblyName = context.Compilation.AssemblyName ?? string.Empty;
		var syntaxReferences = typeSymbol.DeclaringSyntaxReferences
			.OrderBy(reference => reference.SyntaxTree.FilePath, StringComparer.Ordinal)
			.ThenBy(reference => reference.Span.Start)
			.ToArray();

		var normalizedPathCache = new Dictionary<(string FilePath, SourceLocationBase RelativeTo, string BasePath), string>();
		foreach (var syntaxReference in syntaxReferences)
		{
			var syntax = syntaxReference.GetSyntax(context.CancellationToken);
			if (GeneratedCodeDetector.IsGenerated(syntax.SyntaxTree, context.CancellationToken))
			{
				continue;
			}

			var identifierLocation = GetIdentifierLocation(syntax);
			foreach (var policy in policies)
			{
				if (!TryNormalizeSourcePath(syntax.SyntaxTree.FilePath, policy, buildProperties, normalizedPathCache, out var normalizedSourcePath, out var reason))
				{
					Report(context, typeSymbol.Name, layerMatch.Value.Layer.Name, syntax.SyntaxTree.FilePath ?? string.Empty, normalizedSourcePath, assemblyName, identifierLocation, policy, reason);
					break;
				}

				if (policy.Matches(normalizedSourcePath, assemblyName))
				{
					continue;
				}

				var violationReason = $"source file '{normalizedSourcePath}' does not match an allowed SourceLocations rule for layer '{policy.OwnerLayerPath}'";
				Report(context, typeSymbol.Name, layerMatch.Value.Layer.Name, syntax.SyntaxTree.FilePath ?? string.Empty, normalizedSourcePath, assemblyName, identifierLocation, policy, violationReason);
				break;
			}
		}
	}

	private static bool TryNormalizeSourcePath(string? filePath, SourceLocationPolicy policy, AnalyzerBuildProperties buildProperties, IDictionary<(string FilePath, SourceLocationBase RelativeTo, string BasePath), string> cache, out string normalizedSourcePath, out string reason)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			normalizedSourcePath = string.Empty;
			reason = policy.RelativeTo == SourceLocationBase.Absolute
				? "the source location cannot be evaluated because the syntax tree has no FilePath"
				: string.Empty;
			return policy.RelativeTo != SourceLocationBase.Absolute;
		}

		var sourceFilePath = filePath!;
		var basePath = GetBasePath(policy, buildProperties);
		if (basePath is null)
		{
			normalizedSourcePath = string.Empty;
			reason = $"the source location cannot be evaluated because relativeTo='{policy.RelativeTo}' has no usable base path";
			return false;
		}

		var cacheKey = (sourceFilePath, policy.RelativeTo, basePath);
		if (!cache.TryGetValue(cacheKey, out normalizedSourcePath!))
		{
			normalizedSourcePath = policy.RelativeTo == SourceLocationBase.Absolute
				? SourcePathNormalizer.NormalizeAbsolute(sourceFilePath)
				: SourcePathNormalizer.NormalizeRelativeToBase(sourceFilePath, basePath);
			cache[cacheKey] = normalizedSourcePath;
		}

		reason = string.Empty;
		return true;
	}

	private static string? GetBasePath(SourceLocationPolicy policy, AnalyzerBuildProperties buildProperties)
	{
		switch (policy.RelativeTo)
		{
			case SourceLocationBase.Project:
				return buildProperties.ProjectDirectory;
			case SourceLocationBase.Configuration:
				return Path.GetDirectoryName(policy.XmlPath);
			case SourceLocationBase.Absolute:
				return string.Empty;
			default:
				return null;
		}
	}

	private static void Report(SymbolAnalysisContext context, string typeName, string layerName, string sourceFilePath, string normalizedSourcePath, string assemblyName, Location location, SourceLocationPolicy policy, string reason)
	{
		var properties = ImmutableDictionary<string, string?>.Empty
			.Add(ArchitecturalDiagnostics.PropertyCallerTypeName, typeName)
			.Add(ArchitecturalDiagnostics.PropertyCallerLayerName, layerName)
			.Add(ArchitecturalDiagnostics.PropertySourceFilePath, sourceFilePath)
			.Add(ArchitecturalDiagnostics.PropertyNormalizedSourcePath, normalizedSourcePath)
			.Add(ArchitecturalDiagnostics.PropertySourceAssemblyName, assemblyName)
			.Add(ArchitecturalDiagnostics.PropertyViolationReason, reason)
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlPath, policy.XmlPath)
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, policy.XmlLineNumber.ToString(System.Globalization.CultureInfo.InvariantCulture))
			.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, policy.XmlLinePosition.ToString(System.Globalization.CultureInfo.InvariantCulture));

		context.ReportDiagnostic(Diagnostic.Create(
			ArchitecturalDiagnostics.SourceLocationViolation,
			location,
			properties,
			typeName,
			layerName,
			normalizedSourcePath,
			policy.OwnerLayerPath));
	}

	private static Location GetIdentifierLocation(SyntaxNode syntax)
	{
		var result = syntax switch
		{
			BaseTypeDeclarationSyntax declaration => declaration.Identifier.GetLocation(),
			DelegateDeclarationSyntax declaration => declaration.Identifier.GetLocation(),
			_ => syntax.GetLocation()
		};

		return result;
	}
}
