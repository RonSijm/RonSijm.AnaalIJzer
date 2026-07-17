using System.Collections.Immutable;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

internal static class InlineAssemblyMetadataSettings
{
	internal static bool TryFindInlineSettings(string source, out InlineSettingsLiteral settings, out string message)
	{
		var tree = CSharpSyntaxTree.ParseText(source);
		var root = tree.GetRoot();
		foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>())
		{
			if (!IsAssemblyMetadataAttribute(attribute))
			{
				continue;
			}

			var arguments = attribute.ArgumentList?.Arguments;
			if (arguments is null || arguments.Value.Count < 2 || !IsAnaalIJzerSettingsKey(arguments.Value[0]))
			{
				continue;
			}

			return TryReadEditableXmlLiteral(arguments.Value[1].Expression, out settings, out message);
		}

		settings = default;
		message = "Could not find AssemblyMetadata(\"AnaalIJzerSettings\", ...) in " + "the inline settings source file.";
		return false;
	}

	internal static bool IsAssemblyMetadataAttribute(AttributeSyntax attribute)
	{
		if (attribute.Parent is not AttributeListSyntax { Target.Identifier.ValueText: "assembly" })
		{
			return false;
		}

		var name = attribute.Name.ToString();
		var result = name.EndsWith("AssemblyMetadata", StringComparison.Ordinal)
		             || name.EndsWith("AssemblyMetadataAttribute", StringComparison.Ordinal);

		return result;
	}

	internal static bool IsAnaalIJzerSettingsKey(AttributeArgumentSyntax argument)
	{
		var result = argument.Expression is LiteralExpressionSyntax literal
		             && string.Equals(literal.Token.ValueText, "AnaalIJzerSettings", StringComparison.Ordinal);

		return result;
	}

	internal static bool TryReadEditableXmlLiteral(ExpressionSyntax expression, out InlineSettingsLiteral settings, out string message)
	{
		if (expression is LiteralExpressionSyntax literal)
		{
			settings = InlineSettingsLiteral.ForRawLiteral(literal.Span, literal.Token.ValueText);
			message = string.Empty;
			return true;
		}

		if (expression is InterpolatedStringExpressionSyntax interpolated)
		{
			return TryReadInterpolatedXmlLiteral(interpolated, out settings, out message);
		}

		settings = default;
		message = "Inline AnaalIJzer settings are not a directly editable string literal.";
		return false;
	}

	internal static bool TryReadInterpolatedXmlLiteral(InterpolatedStringExpressionSyntax interpolated, out InlineSettingsLiteral settings, out string message)
	{
		var xml = new StringBuilder();
		var placeholderXml = new StringBuilder();
		var interpolations = ImmutableArray.CreateBuilder<InlineInterpolation>();
		foreach (var content in interpolated.Contents)
		{
			if (content is InterpolatedStringTextSyntax text)
			{
				xml.Append(text.TextToken.ValueText);
				placeholderXml.Append(text.TextToken.ValueText);
				continue;
			}

			if (content is not InterpolationSyntax interpolation)
			{
				settings = default;
				message = "Inline AnaalIJzer settings contain unsupported interpolation content.";
				return false;
			}

			if (!TryEvaluateInlineInterpolation(interpolation, out var value, out message))
			{
				settings = default;
				return false;
			}

			var marker = "__ANAALIJZER_INTERPOLATION_" + interpolations.Count + "__";
			xml.Append(value);
			placeholderXml.Append(marker);
			interpolations.Add(new InlineInterpolation(marker, interpolation.ToString(), value));
		}

		settings = InlineSettingsLiteral.ForInterpolatedLiteral(interpolated.Span, xml.ToString(), placeholderXml.ToString(), interpolations.ToImmutable());
		message = string.Empty;
		return true;
	}

	internal static bool TryEvaluateInlineInterpolation(InterpolationSyntax interpolation, out string value, out string message)
	{
		if (interpolation.AlignmentClause is not null || interpolation.FormatClause is not null)
		{
			value = string.Empty;
			message = "Inline AnaalIJzer settings use interpolation alignment or formatting. Only plain nameof(...) interpolation can be edited safely.";
			return false;
		}

		if (TryEvaluateNameof(interpolation.Expression, out value))
		{
			message = string.Empty;
			return true;
		}

		if (interpolation.Expression is LiteralExpressionSyntax literal && literal.Kind() == SyntaxKind.StringLiteralExpression)
		{
			value = literal.Token.ValueText;
			message = string.Empty;
			return true;
		}

		value = string.Empty;
		message = "Inline AnaalIJzer settings use unsupported interpolation '" + interpolation.Expression + "'. Only nameof(...) and string literal interpolation can be edited safely.";
		return false;
	}

	internal static bool TryEvaluateNameof(ExpressionSyntax expression, out string value)
	{
		if (expression is not InvocationExpressionSyntax invocation
		    || invocation.Expression is not IdentifierNameSyntax { Identifier.ValueText: "nameof" }
		    || invocation.ArgumentList.Arguments.Count != 1)
		{
			value = string.Empty;
			return false;
		}

		value = GetNameofValue(invocation.ArgumentList.Arguments[0].Expression);
		var result = value.Length > 0;

		return result;
	}

	internal static string GetNameofValue(ExpressionSyntax expression)
	{
		var result = expression switch
		{
			IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
			GenericNameSyntax generic => generic.Identifier.ValueText,
			QualifiedNameSyntax qualified => GetNameofValue(qualified.Right),
			AliasQualifiedNameSyntax aliasQualified => GetNameofValue(aliasQualified.Name),
			MemberAccessExpressionSyntax memberAccess => GetNameofValue(memberAccess.Name),
			_ => string.Empty
		};

		return result;
	}

	internal static bool TryCreateInlineSettingsLiteral(InlineSettingsLiteral settings, string updatedXml, string newLine, out string literal, out string message)
	{
		if (!settings.IsInterpolated)
		{
			literal = CreateRawStringLiteral(updatedXml, newLine);
			message = string.Empty;
			return true;
		}

		if (!TryRestoreInterpolatedXml(settings, updatedXml, out var interpolatedXml, out message))
		{
			literal = string.Empty;
			return false;
		}

		literal = "$" + CreateRawStringLiteral(interpolatedXml, newLine);
		message = string.Empty;
		return true;
	}

	internal static bool TryRestoreInterpolatedXml(InlineSettingsLiteral settings, string updatedXml, out string interpolatedXml, out string message)
	{
		string normalizedTemplate;
		try
		{
			normalizedTemplate = ArchitectureConfigurationXmlSerializer.SerializeXml(XDocument.Parse(settings.PlaceholderXml, LoadOptions.PreserveWhitespace));
		}
		catch (XmlException exception)
		{
			interpolatedXml = string.Empty;
			message = "Inline AnaalIJzer settings interpolation could not be restored because the placeholder XML is invalid: " + exception.Message;
			return false;
		}

		var builder = new StringBuilder(updatedXml);
		var searchPosition = 0;
		foreach (var interpolation in settings.Interpolations)
		{
			var markerIndex = normalizedTemplate.IndexOf(interpolation.Marker, StringComparison.Ordinal);
			if (markerIndex < 0)
			{
				interpolatedXml = string.Empty;
				message = "Inline AnaalIJzer settings interpolation marker was not found after XML normalization.";
				return false;
			}

			var prefix = GetInterpolationPrefix(normalizedTemplate, markerIndex);
			var currentText = builder.ToString();
			var prefixIndex = currentText.IndexOf(prefix, searchPosition, StringComparison.Ordinal);
			if (prefixIndex < 0)
			{
				interpolatedXml = string.Empty;
				message = "Inline AnaalIJzer settings interpolation could not be matched after the XML edit.";
				return false;
			}

			var valueIndex = prefixIndex + prefix.Length;
			if (currentText.IndexOf(interpolation.Value, valueIndex, StringComparison.Ordinal) == valueIndex)
			{
				builder.Remove(valueIndex, interpolation.Value.Length);
				builder.Insert(valueIndex, interpolation.SourceText);
				searchPosition = valueIndex + interpolation.SourceText.Length;
				continue;
			}

			searchPosition = valueIndex;
		}

		interpolatedXml = builder.ToString();
		message = string.Empty;
		return true;
	}

	internal static string GetInterpolationPrefix(string template, int markerIndex)
	{
		var quoteStart = markerIndex > 0 ? template.LastIndexOf('"', markerIndex - 1) : -1;
		var quoteEnd = template.IndexOf('"', markerIndex);
		if (quoteStart >= 0 && quoteEnd > markerIndex)
		{
			var attributeStart = LastIndexOfAny(template, new[] { '<', ' ', '\t', '\r', '\n' }, quoteStart - 1);
			if (attributeStart >= 0)
			{
				var attributePrefix = template.Substring(attributeStart + 1, markerIndex - attributeStart - 1);

				return attributePrefix;
			}
		}

		var start = Math.Max(0, markerIndex - 40);
		var result = template.Substring(start, markerIndex - start);

		return result;
	}

	internal static int LastIndexOfAny(string value, char[] characters, int startIndex)
	{
		if (startIndex < 0)
		{
			return -1;
		}

		for (var index = Math.Min(startIndex, value.Length - 1); index >= 0; index--)
		{
			if (characters.Contains(value[index]))
			{
				return index;
			}
		}

		return -1;
	}

	internal static string CreateRawStringLiteral(string xml, string newLine)
	{
		var delimiter = new string('"', Math.Max(3, GetLongestQuoteRun(xml) + 1));
		var result = delimiter + newLine + xml + newLine + delimiter;

		return result;
	}

	internal static int GetLongestQuoteRun(string value)
	{
		var longest = 0;
		var current = 0;
		foreach (var character in value)
		{
			if (character == '"')
			{
				current++;
				longest = Math.Max(longest, current);
			}
			else
			{
				current = 0;
			}
		}

		return longest;
	}

	internal static string DetectNewLine(string source)
	{
		var result = source.Contains("\r\n") ? "\r\n" : "\n";

		return result;
	}

	internal readonly struct InlineSettingsLiteral
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

		internal static InlineSettingsLiteral ForRawLiteral(TextSpan literalSpan, string xml)
		{
			var result = new InlineSettingsLiteral(literalSpan, xml, xml, ImmutableArray<InlineInterpolation>.Empty);

			return result;
		}

		internal static InlineSettingsLiteral ForInterpolatedLiteral(TextSpan literalSpan, string xml, string placeholderXml, ImmutableArray<InlineInterpolation> interpolations)
		{
			var result = new InlineSettingsLiteral(literalSpan, xml, placeholderXml, interpolations);

			return result;
		}
	}

	internal readonly struct InlineInterpolation(string marker, string sourceText, string value)
	{
		public string Marker { get; } = marker;

		public string SourceText { get; } = sourceText;

		public string Value { get; } = value;
	}
}
