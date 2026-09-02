using System.Collections.Immutable;
using System.Globalization;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Model;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class CycleDependencyCodeFix
{
	private const char CandidateSeparator = '\u001e';
	private const char FieldSeparator = '\u001f';

	internal static async Task TryRegisterAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		var candidates = ReadCandidates(diagnostic);
		if (candidates.IsDefaultOrEmpty)
		{
			return;
		}

		var snapshots = await ConfigurationCodeFixSupport.GetConfigurationSnapshotsAsync(context.Document, context.CancellationToken).ConfigureAwait(false);
		var discoveredSource = await ConfigurationCodeFixSupport.FindDefaultConfigurationSourceAsync(context.Document, context.CancellationToken).ConfigureAwait(false);
		foreach (var candidate in candidates)
		{
			var source = ConfigurationCodeFixSupport.ResolveSource(discoveredSource, candidate.SourcePath, snapshots);
			if (!source.CanEdit)
			{
				continue;
			}

			RegisterBlockFix(context, diagnostic, source, candidate);
			RegisterRemoveFix(context, diagnostic, source, candidate);
		}
	}

	private static void RegisterBlockFix(CodeFixContext context, Diagnostic diagnostic, ArchitectureConfigurationSource source, CycleRuleCandidate candidate)
	{
		var title = $"Break configured cycle by blocking '{candidate.ConfiguredFrom}' -> '{candidate.ConfiguredTo}'";
		context.RegisterCodeFix(
			CodeAction.Create(
				title,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					source,
					document => TryAddBlockingDependency(document, candidate),
					cancellationToken),
				title),
			diagnostic);
	}

	private static void RegisterRemoveFix(CodeFixContext context, Diagnostic diagnostic, ArchitectureConfigurationSource source, CycleRuleCandidate candidate)
	{
		var title = $"Break configured cycle by removing allowed dependency '{candidate.ConfiguredFrom}' -> '{candidate.ConfiguredTo}'";
		context.RegisterCodeFix(
			CodeAction.Create(
				title,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					source,
					document => TryRemoveAllowedDependency(document, candidate),
					cancellationToken),
				title),
			diagnostic);
	}

	private static ImmutableArray<CycleRuleCandidate> ReadCandidates(Diagnostic diagnostic)
	{
		var serialized = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyCycleRuleCandidates);
		if (string.IsNullOrWhiteSpace(serialized))
		{
			return [];
		}

		var builder = ImmutableArray.CreateBuilder<CycleRuleCandidate>();
		foreach (var candidateText in serialized.Split(CandidateSeparator))
		{
			var fields = candidateText.Split(FieldSeparator);
			if (fields.Length != 8
			    || string.IsNullOrWhiteSpace(fields[0])
			    || string.IsNullOrWhiteSpace(fields[4])
			    || string.IsNullOrWhiteSpace(fields[5]))
			{
				continue;
			}

			if (!int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var lineNumber)
			    || !int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var linePosition))
			{
				continue;
			}

			builder.Add(new CycleRuleCandidate(fields[0], fields[3], fields[4], fields[5], lineNumber, linePosition));
		}

		var result = builder
			.Distinct()
			.ToImmutableArray();

		return result;
	}

	private static bool TryAddBlockingDependency(XDocument document, CycleRuleCandidate candidate)
	{
		var allowedDependency = FindAllowedDependency(document, candidate);
		if (allowedDependency?.Parent is not XElement parent)
		{
			return false;
		}

		var existingBlock = parent.Elements("BlockedDependency").Any(element =>
			string.Equals(element.Attribute("from")?.Value, candidate.ConfiguredFrom, StringComparison.Ordinal)
			&& string.Equals(element.Attribute("to")?.Value, candidate.ConfiguredTo, StringComparison.Ordinal));
		if (existingBlock)
		{
			return false;
		}

		allowedDependency.AddAfterSelf(new XElement(
			"BlockedDependency",
			new XAttribute("from", candidate.ConfiguredFrom),
			new XAttribute("to", candidate.ConfiguredTo)));

		return true;
	}

	private static bool TryRemoveAllowedDependency(XDocument document, CycleRuleCandidate candidate)
	{
		var allowedDependency = FindAllowedDependency(document, candidate);
		if (allowedDependency is null)
		{
			return false;
		}

		allowedDependency.Remove();
		return true;
	}

	private static XElement? FindAllowedDependency(XDocument document, CycleRuleCandidate candidate)
	{
		var candidates = document.Descendants("AllowedDependency").ToArray();
		var byLine = ConfigurationCodeFixSupport.FindElementByLineInfo(candidates, candidate.LineNumber, candidate.LinePosition);
		if (byLine is not null)
		{
			return byLine;
		}

		var result = candidates.FirstOrDefault(element =>
			string.Equals(element.Attribute("from")?.Value, candidate.ConfiguredFrom, StringComparison.Ordinal)
			&& string.Equals(element.Attribute("to")?.Value, candidate.ConfiguredTo, StringComparison.Ordinal)
			&& IsInScope(element, candidate.ScopePath));

		return result;
	}

	private static bool IsInScope(XElement element, string scopePath)
	{
		if (string.IsNullOrWhiteSpace(scopePath))
		{
			return element.Parent?.Name.LocalName == "ArchitecturalLevels";
		}

		var ancestorNames = element.Ancestors("Layer")
			.Select(ancestor => ancestor.Attribute("name")?.Value)
			.Reverse()
			.Where(name => !string.IsNullOrWhiteSpace(name));
		var path = string.Join("/", ancestorNames);
		var result = string.Equals(path, scopePath, StringComparison.Ordinal);

		return result;
	}

	private readonly struct CycleRuleCandidate(
		string sourcePath,
		string scopePath,
		string configuredFrom,
		string configuredTo,
		int lineNumber,
		int linePosition) : IEquatable<CycleRuleCandidate>
	{
		public string SourcePath { get; } = sourcePath;

		public string ScopePath { get; } = scopePath;

		public string ConfiguredFrom { get; } = configuredFrom;

		public string ConfiguredTo { get; } = configuredTo;

		public int LineNumber { get; } = lineNumber;

		public int LinePosition { get; } = linePosition;

		public bool Equals(CycleRuleCandidate other)
		{
			var result = string.Equals(SourcePath, other.SourcePath, StringComparison.Ordinal)
			             && string.Equals(ScopePath, other.ScopePath, StringComparison.Ordinal)
			             && string.Equals(ConfiguredFrom, other.ConfiguredFrom, StringComparison.Ordinal)
			             && string.Equals(ConfiguredTo, other.ConfiguredTo, StringComparison.Ordinal)
			             && LineNumber == other.LineNumber
			             && LinePosition == other.LinePosition;

			return result;
		}

		public override bool Equals(object? obj)
		{
			var result = obj is CycleRuleCandidate other && Equals(other);

			return result;
		}

		public override int GetHashCode()
		{
			var hash = 17;
			hash = hash * 31 + StringComparer.Ordinal.GetHashCode(SourcePath);
			hash = hash * 31 + StringComparer.Ordinal.GetHashCode(ScopePath);
			hash = hash * 31 + StringComparer.Ordinal.GetHashCode(ConfiguredFrom);
			hash = hash * 31 + StringComparer.Ordinal.GetHashCode(ConfiguredTo);
			hash = hash * 31 + LineNumber;
			hash = hash * 31 + LinePosition;

			return hash;
		}
	}
}
