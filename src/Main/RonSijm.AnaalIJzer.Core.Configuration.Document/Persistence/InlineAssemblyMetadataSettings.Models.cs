using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Persistence;

public static partial class InlineAssemblyMetadataSettings
{
	public readonly struct InlineSettingsLiteral
	{
		private InlineSettingsLiteral(TextSpan literalSpan, string xml, string placeholderXml, ImmutableArray<InlineInterpolation> interpolations)
		{
			LiteralSpan = literalSpan;
			Xml = xml;
			PlaceholderXml = placeholderXml;
			Interpolations = interpolations;
		}

		public TextSpan LiteralSpan { get; }

		public string Xml { get; }

		public string PlaceholderXml { get; }

		public ImmutableArray<InlineInterpolation> Interpolations { get; }

		public bool IsInterpolated => Interpolations.Length > 0;

		public static InlineSettingsLiteral ForRawLiteral(TextSpan literalSpan, string xml)
		{
			var result = new InlineSettingsLiteral(literalSpan, xml, xml, ImmutableArray<InlineInterpolation>.Empty);

			return result;
		}

		public static InlineSettingsLiteral ForInterpolatedLiteral(TextSpan literalSpan, string xml, string placeholderXml, ImmutableArray<InlineInterpolation> interpolations)
		{
			var result = new InlineSettingsLiteral(literalSpan, xml, placeholderXml, interpolations);

			return result;
		}
	}

	public readonly struct InlineInterpolation(string marker, string sourceText, string value)
	{
		public string Marker { get; } = marker;

		public string SourceText { get; } = sourceText;

		public string Value { get; } = value;
	}
}
