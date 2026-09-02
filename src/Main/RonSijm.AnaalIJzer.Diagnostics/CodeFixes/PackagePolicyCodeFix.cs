using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace RonSijm.AnaalIJzer.Diagnostics;

internal static class PackagePolicyCodeFix
{
	private const string AllowedListReasonPrefix = "the package does not match the Allowed package list for project group '";

	internal static async Task TryRegisterAsync(CodeFixContext context, Diagnostic diagnostic)
	{
		var sourceProjectGroup = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertySourceProjectGroup);
		var packageId = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyPackageId);
		var violationReason = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyViolationReason);
		var sourcePath = ConfigurationCodeFixSupport.ReadStringProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlPath);
		var xmlLineNumber = ConfigurationCodeFixSupport.ReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlLine);
		var xmlLinePosition = ConfigurationCodeFixSupport.ReadIntProperty(diagnostic, ArchitecturalDiagnostics.PropertyRuleXmlCol);
		if (string.IsNullOrWhiteSpace(sourceProjectGroup)
		    || string.IsNullOrWhiteSpace(packageId)
		    || string.IsNullOrWhiteSpace(sourcePath)
		    || xmlLineNumber <= 0
		    || !IsAllowedListFailure(violationReason, sourceProjectGroup))
		{
			return;
		}

		var discoveredSource = await ConfigurationCodeFixSupport.FindDefaultConfigurationSourceAsync(context.Document, context.CancellationToken).ConfigureAwait(false);
		var snapshots = await ConfigurationCodeFixSupport.GetConfigurationSnapshotsAsync(context.Document, context.CancellationToken).ConfigureAwait(false);
		var configurationSource = ConfigurationCodeFixSupport.ResolveSource(discoveredSource, sourcePath, snapshots);
		if (!configurationSource.CanEdit)
		{
			return;
		}

		var title = $"Allow package '{packageId}' for project group '{sourceProjectGroup}'";
		context.RegisterCodeFix(
			CodeAction.Create(
				title,
				cancellationToken => ConfigurationCodeFixEditor.EditConfigurationAsync(
					context.Document,
					configurationSource,
					document => TryAddAllowedPackage(document, xmlLineNumber, xmlLinePosition, packageId),
					cancellationToken),
				title),
			diagnostic);
	}

	private static bool TryAddAllowedPackage(XDocument document, int xmlLineNumber, int xmlLinePosition, string packageId)
	{
		var packagePolicy = ConfigurationCodeFixSupport.FindElementByLineInfo(document, "PackagePolicy", xmlLineNumber, xmlLinePosition);
		if (packagePolicy is null)
		{
			return false;
		}

		var allowedContainer = packagePolicy.Element("Allowed");
		if (allowedContainer is null)
		{
			allowedContainer = new XElement("Allowed");
			var forbiddenContainer = packagePolicy.Element("Forbidden");
			if (forbiddenContainer is not null)
			{
				forbiddenContainer.AddBeforeSelf(allowedContainer);
			}
			else
			{
				packagePolicy.Add(allowedContainer);
			}
		}

		var existingAllowed = allowedContainer.Elements("Package")
			.Any(element =>
				string.Equals(element.Attribute("exactName")?.Value, packageId, StringComparison.Ordinal)
				|| string.Equals(element.Attribute("typeName")?.Value, packageId, StringComparison.Ordinal));
		if (existingAllowed)
		{
			return false;
		}

		allowedContainer.Add(new XElement("Package", new XAttribute("exactName", packageId)));
		return true;
	}

	private static bool IsAllowedListFailure(string violationReason, string sourceProjectGroup)
	{
		var expected = $"{AllowedListReasonPrefix}{sourceProjectGroup}'";
		var result = string.Equals(violationReason, expected, StringComparison.Ordinal);

		return result;
	}
}
