using System.Collections.Immutable;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RonSijm.AnaalIJzer.ConfigurationEditing.Editing;

public static partial class InlineAssemblyMetadataSettings
{
	public static bool TryFindInlineSettings(string source, out InlineSettingsLiteral settings, out string message)
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

	public static bool IsAssemblyMetadataAttribute(AttributeSyntax attribute)
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

	public static bool IsAnaalIJzerSettingsKey(AttributeArgumentSyntax argument)
	{
		var result = argument.Expression is LiteralExpressionSyntax literal
		             && string.Equals(literal.Token.ValueText, "AnaalIJzerSettings", StringComparison.Ordinal);

		return result;
	}

	public static bool TryReadEditableXmlLiteral(ExpressionSyntax expression, out InlineSettingsLiteral settings, out string message)
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

	public static string DetectNewLine(string source)
	{
		var result = source.Contains("\r\n") ? "\r\n" : "\n";

		return result;
	}
}
