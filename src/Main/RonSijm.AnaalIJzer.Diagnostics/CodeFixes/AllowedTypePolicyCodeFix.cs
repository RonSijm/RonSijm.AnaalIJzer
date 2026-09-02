using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class AllowedTypePolicyCodeFix
{
	private const string GlobalAllowedReason = "the global <Allowed> list has no matching rule";
	private const string ScopedAllowedReasonPrefix = "the <Allowed> list scoped to layer '";

	internal static async Task TryRegisterAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		var violationReason = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyViolationReason);
		var dependencyTypeName = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyDepTypeName);
		var dependencyLayerName = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyDepLayerName);
		if (string.IsNullOrWhiteSpace(dependencyTypeName)
		    || string.IsNullOrWhiteSpace(dependencyLayerName)
		    || !IsAllowedListFailure(violationReason))
		{
			return;
		}

		var snapshots = await ConfigurationCodeFixSupport.GetConfigurationSnapshotsAsync(context.Document, context.CancellationToken).ConfigureAwait(false);
		if (snapshots.IsDefaultOrEmpty)
		{
			return;
		}

		var targets = GetApplicableAllowedTargets(snapshots, dependencyLayerName);
		if (targets.IsDefaultOrEmpty)
		{
			return;
		}

		var title = $"Allow '{dependencyTypeName}' in applicable <Allowed> lists";
		context.RegisterCodeFix(
			CodeAction.Create(
				title,
				cancellationToken => ApplyAllowedTypeFixAsync(context.Document, targets, dependencyTypeName, cancellationToken),
				title),
			diagnostic);
	}

	private static async Task<Solution> ApplyAllowedTypeFixAsync(
		Document document,
		ImmutableArray<AllowedContainerTarget> targets,
		string dependencyTypeName,
		CancellationToken cancellationToken)
	{
		var solution = document.Project.Solution;
		foreach (var target in targets)
		{
			var currentDocument = solution.GetDocument(document.Id) ?? document;
			solution = await ConfigurationCodeFixEditor.EditConfigurationAsync(
				currentDocument,
				target.Source,
				config => TryAddAllowedType(config, target.LayerPath, dependencyTypeName),
				cancellationToken).ConfigureAwait(false);
		}

		var result = solution;

		return result;
	}

	private static ImmutableArray<AllowedContainerTarget> GetApplicableAllowedTargets(
		ImmutableArray<ConfigurationCodeFixSupport.ConfigurationDocumentSnapshot> snapshots,
		string callerLayerName)
	{
		var builder = ImmutableArray.CreateBuilder<AllowedContainerTarget>();
		var seenTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var snapshot in snapshots)
		{
			var root = snapshot.Document.Root;
			if (root?.Element("Allowed") is not null)
			{
				AddTarget(builder, seenTargets, snapshot.Source, layerPath: null);
			}

			foreach (var layerPath in ConfigurationCodeFixSupport.GetAncestorLayerPaths(callerLayerName))
			{
				var layerElement = ConfigurationCodeFixSupport.FindLayerElement(snapshot.Document, layerPath);
				if (layerElement?.Element("Allowed") is null)
				{
					continue;
				}

				AddTarget(builder, seenTargets, snapshot.Source, layerPath);
			}
		}

		var result = builder.ToImmutable();

		return result;
	}

	private static bool TryAddAllowedType(XDocument document, string? layerPath, string dependencyTypeName)
	{
		var containerParent = layerPath is null
			? document.Root
			: ConfigurationCodeFixSupport.FindLayerElement(document, layerPath);
		var allowedContainer = containerParent?.Element("Allowed");
		if (allowedContainer is null)
		{
			return false;
		}

		var existing = allowedContainer.Elements("Class")
			.Any(element =>
				string.Equals(element.Attribute("typeName")?.Value, dependencyTypeName, StringComparison.Ordinal)
				|| string.Equals(element.Attribute("exactName")?.Value, dependencyTypeName, StringComparison.Ordinal));
		if (existing)
		{
			return false;
		}

		allowedContainer.Add(new XElement("Class", new XAttribute("typeName", dependencyTypeName)));
		return true;
	}

	private static void AddTarget(
		ImmutableArray<AllowedContainerTarget>.Builder builder,
		HashSet<string> seenTargets,
		RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource source,
		string? layerPath)
	{
		var key = source.Kind + "|" + source.Path + "|" + (layerPath ?? "<root>");
		if (!seenTargets.Add(key))
		{
			return;
		}

		builder.Add(new AllowedContainerTarget(source, layerPath));
	}

	private static bool IsAllowedListFailure(string violationReason)
	{
		var result = string.Equals(violationReason, GlobalAllowedReason, StringComparison.Ordinal)
		             || violationReason.StartsWith(ScopedAllowedReasonPrefix, StringComparison.Ordinal);

		return result;
	}

	private readonly struct AllowedContainerTarget(
		RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource source,
		string? layerPath)
	{
		public RonSijm.AnaalIJzer.Core.Configuration.Document.Model.ArchitectureConfigurationSource Source { get; } = source;

		public string? LayerPath { get; } = layerPath;
	}
}
