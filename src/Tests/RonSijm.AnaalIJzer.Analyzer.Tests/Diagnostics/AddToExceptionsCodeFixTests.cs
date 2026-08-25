using System.Collections.Immutable;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using RonSijm.AnaalIJzer.Exceptions;
using RonSijm.AnaalIJzer.Testing;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Diagnostics;

[Collection(ArchitectureClockTestCollection.Name)]
public sealed class AddToExceptionsCodeFixTests
{
	[Fact]
	public void AddToExceptionsCodeFix_DirectCall_AppendsClassUnderTargetRule()
	{
		const string xml = """
		                   <?xml version="1.0" encoding="utf-8"?>
		                   <ArchitecturalLevels>
		                     <Forbidden>
		                       <Class endsWith="Store" />
		                     </Forbidden>
		                   </ArchitecturalLevels>
		                   """;

		var doc = XDocument.Parse(xml, LoadOptions.SetLineInfo);
		var classEl = doc.Descendants("Class").Single();
		var info = (IXmlLineInfo)classEl;

		var updated = AddToExceptionsCodeFix.AddException(
			SourceText.From(xml), info.LineNumber, info.LinePosition, "LegacyPatientStore");

		updated.Should().NotBeNull();

		var reparsed = XDocument.Parse(updated!.ToString());
		var exceptions = reparsed.Descendants("Class")
			.Single(c => c.Attribute("endsWith")?.Value == "Store")
			.Element("Exceptions");

		exceptions.Should().NotBeNull();
		exceptions!.Elements("Class")
			.Where(c => c.Attribute("typeName")?.Value == "LegacyPatientStore")
			.Should().ContainSingle();
	}

	[Fact]
	public void AddToExceptionsCodeFix_DirectCall_NoOpWhenAlreadyExcepted()
	{
		const string xml = """
		                   <?xml version="1.0" encoding="utf-8"?>
		                   <ArchitecturalLevels>
		                     <Forbidden>
		                       <Class endsWith="Store">
		                         <Exceptions>
		                           <Class typeName="LegacyPatientStore" />
		                         </Exceptions>
		                       </Class>
		                     </Forbidden>
		                   </ArchitecturalLevels>
		                   """;

		var doc = XDocument.Parse(xml, LoadOptions.SetLineInfo);
		var classEl = doc.Descendants("Class").First(c => c.Attribute("endsWith") is not null);
		var info = (IXmlLineInfo)classEl;

		var updated = AddToExceptionsCodeFix.AddException(
			SourceText.From(xml), info.LineNumber, info.LinePosition, "LegacyPatientStore");

		updated.Should().BeNull();
	}

	[Fact]
	public void AddToExceptionsCodeFix_DirectCall_AppendsToExistingExceptionsWhenTypeIsNew()
	{
		const string xml = """
		                   <?xml version="1.0" encoding="utf-8"?>
		                   <ArchitecturalLevels>
		                     <Forbidden>
		                       <Class endsWith="Store">
		                         <Exceptions>
		                           <Class typeName="LegacyPatientStore" />
		                         </Exceptions>
		                       </Class>
		                     </Forbidden>
		                   </ArchitecturalLevels>
		                   """;

		var doc = XDocument.Parse(xml, LoadOptions.SetLineInfo);
		var classEl = doc.Descendants("Class").First(c => c.Attribute("endsWith") is not null);
		var info = (IXmlLineInfo)classEl;

		var updated = AddToExceptionsCodeFix.AddException(
			SourceText.From(xml), info.LineNumber, info.LinePosition, "PartnerPatientStore");

		updated.Should().NotBeNull();

		var reparsed = XDocument.Parse(updated!.ToString());
		var exceptions = reparsed.Descendants("Class")
			.Single(c => c.Attribute("endsWith")?.Value == "Store")
			.Element("Exceptions");

		exceptions.Should().NotBeNull();
		exceptions!.Elements("Class")
			.Select(c => c.Attribute("typeName")?.Value)
			.Should().Contain(["LegacyPatientStore", "PartnerPatientStore"]);
	}

	[Fact]
	public void AddToExceptionsCodeFix_DirectCall_ReturnsNullWhenElementNotFound()
	{
		const string xml = """
		                   <?xml version="1.0" encoding="utf-8"?>
		                   <ArchitecturalLevels>
		                     <Forbidden>
		                       <Class endsWith="Store" />
		                     </Forbidden>
		                   </ArchitecturalLevels>
		                   """;

		var updated = AddToExceptionsCodeFix.AddException(
			SourceText.From(xml), line: 999, column: 999, depTypeName: "AnyType");

		updated.Should().BeNull();
	}

	[Fact]
	public void AddToExceptionsCodeFix_DirectCall_ReturnsNullWhenXmlIsMalformed()
	{
		var updated = AddToExceptionsCodeFix.AddException(
			SourceText.From("<ArchitecturalLevels>"), line: 1, column: 1, depTypeName: "AnyType");

		updated.Should().BeNull();
	}

	[Fact]
	public void AddToExceptionsCodeFix_DirectCall_WithExceptionPolicy_AddsReviewPlaceholders()
	{
		using var _ = ArchitectureClock.Freeze(new DateTime(2026, 7, 26, 0, 0, 0, DateTimeKind.Utc));

		const string xml = """
		                   <?xml version="1.0" encoding="utf-8"?>
		                   <ArchitecturalLevels>
		                     <ExceptionPolicy requireReason="true"
		                                      requireOwner="true"
		                                      requireExpiresOn="true" />
		                     <Forbidden>
		                       <Class endsWith="Store" />
		                     </Forbidden>
		                   </ArchitecturalLevels>
		                   """;

		var doc = XDocument.Parse(xml, LoadOptions.SetLineInfo);
		var classEl = doc.Descendants("Class").Single();
		var info = (IXmlLineInfo)classEl;

		var updated = AddToExceptionsCodeFix.AddException(SourceText.From(xml), info.LineNumber, info.LinePosition, "LegacyPatientStore");

		updated.Should().NotBeNull();

		var reparsed = XDocument.Parse(updated!.ToString());
		var exceptionElement = reparsed.Descendants("Class")
			.Single(c => c.Attribute("typeName")?.Value == "LegacyPatientStore");

		exceptionElement.Attribute("reason")?.Value.Should().Be("TODO: explain this exception");
		exceptionElement.Attribute("owner")?.Value.Should().Be("TODO: assign an owner");
		exceptionElement.Attribute("expiresOn")?.Value.Should().Be("2026-08-25");
	}

	[Fact]
	public void TryReadRuleLocation_ReturnsFalseWhenDependencyTypeIsMissing()
	{
		var diagnostic = CreateDiagnosticWithProperties(
			ImmutableDictionary<string, string?>.Empty
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, "1")
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, "1"));

		AddToExceptionsCodeFix.TryReadRuleLocation(diagnostic, out _, out _, out _)
			.Should().BeFalse();
	}

	[Fact]
	public void TryReadRuleLocation_ReturnsFalseWhenLineIsMissingOrInvalid()
	{
		var diagnostic = CreateDiagnosticWithProperties(
			ImmutableDictionary<string, string?>.Empty
				.Add(ArchitecturalDiagnostics.PropertyDepTypeName, "CheeseRepository")
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, "nope")
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, "1"));

		AddToExceptionsCodeFix.TryReadRuleLocation(diagnostic, out _, out _, out _)
			.Should().BeFalse();
	}

	[Fact]
	public void TryReadRuleLocation_ReturnsFalseWhenColumnIsMissingOrInvalid()
	{
		var diagnostic = CreateDiagnosticWithProperties(
			ImmutableDictionary<string, string?>.Empty
				.Add(ArchitecturalDiagnostics.PropertyDepTypeName, "CheeseRepository")
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlLine, "1")
				.Add(ArchitecturalDiagnostics.PropertyRuleXmlCol, "0"));

		AddToExceptionsCodeFix.TryReadRuleLocation(diagnostic, out _, out _, out _)
			.Should().BeFalse();
	}

	[Fact]
	public async Task AddToExceptionsCodeFix_AppliedToARCH003_UpdatesAdditionalDocument()
	{
		const string config = """
		                      <?xml version="1.0" encoding="utf-8"?>
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                          <Forbidden>
		                              <Class endsWith="Store" />
		                          </Forbidden>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IPaymentStore { }
		                      public class PaymentManager(IPaymentStore store) { }
		                      """;

		var updatedXml = await AnalyzerTestHelper.ApplyAddToExceptionsCodeFixAsync(
			source, config, ArchitecturalDiagnosticIds.ForbiddenDependency);

		var reparsed = XDocument.Parse(updatedXml);
		var exceptions = reparsed.Descendants("Class")
			.Single(c => c.Attribute("endsWith")?.Value == "Store")
			.Element("Exceptions");

		exceptions.Should().NotBeNull();
		exceptions!.Elements("Class")
			.Where(c => c.Attribute("typeName")?.Value == "IPaymentStore")
			.Should().ContainSingle();
	}

	[Fact]
	public async Task AddToExceptionsCodeFix_AppliedToARCH005_UpdatesAdditionalDocument()
	{
		// Verifies the code fix is wired for every ARCH ID it claims to cover, not
		// only ARCH003. The target rule here is the Manager layer rule itself.
		const string config = """
		                      <?xml version="1.0" encoding="utf-8"?>
		                      <ArchitecturalLevels>
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IPatientManager { }
		                      public class OrderManager(IPatientManager other) { }
		                      """;

		var updatedXml = await AnalyzerTestHelper.ApplyAddToExceptionsCodeFixAsync(
			source, config, ArchitecturalDiagnosticIds.SameLayerDependency);

		var reparsed = XDocument.Parse(updatedXml);
		var exceptions = reparsed.Descendants("Class")
			.Single(c => c.Attribute("endsWith")?.Value == "Manager")
			.Element("Exceptions");

		exceptions.Should().NotBeNull();
		exceptions!.Elements("Class")
			.Where(c => c.Attribute("typeName")?.Value == "IPatientManager")
			.Should().ContainSingle();
	}

	[Fact]
	public async Task AddToExceptionsCodeFix_AppliedWithExceptionPolicy_UsesReviewPlaceholders()
	{
		using var _ = ArchitectureClock.Freeze(new DateTime(2026, 7, 26, 0, 0, 0, DateTimeKind.Utc));

		const string config = """
		                      <?xml version="1.0" encoding="utf-8"?>
		                      <ArchitecturalLevels>
		                          <ExceptionPolicy requireReason="true"
		                                           requireOwner="true"
		                                           requireExpiresOn="true" />
		                          <Layer name="Application">
		                              <Class endsWith="Manager" />
		                          </Layer>
		                      </ArchitecturalLevels>
		                      """;

		const string source = """
		                      public interface IPatientManager { }
		                      public class OrderManager(IPatientManager other) { }
		                      """;

		var updatedXml = await AnalyzerTestHelper.ApplyAddToExceptionsCodeFixAsync(
			source, config, ArchitecturalDiagnosticIds.SameLayerDependency);

		var reparsed = XDocument.Parse(updatedXml);
		var exceptionElement = reparsed.Descendants("Class")
			.Single(c => c.Attribute("typeName")?.Value == "IPatientManager");

		exceptionElement.Attribute("reason")?.Value.Should().Be("TODO: explain this exception");
		exceptionElement.Attribute("owner")?.Value.Should().Be("TODO: assign an owner");
		exceptionElement.Attribute("expiresOn")?.Value.Should().Be("2026-08-25");
	}

	[Fact]
	public async Task AddToExceptionsCodeFix_AppliedToIncludedRule_UpdatesIncludedDocument()
	{
		const string parentConfig = """
		                            <?xml version="1.0" encoding="utf-8"?>
		                            <ArchitecturalLevels>
		                                <Include path="SharedPizzeriaLayers.xml" />
		                            </ArchitecturalLevels>
		                            """;

		const string sharedConfig = """
		                            <?xml version="1.0" encoding="utf-8"?>
		                            <ArchitecturalLevels>
		                                <Layer name="Controller">
		                                    <Class endsWith="Controller" />
		                                </Layer>
		                                <Layer name="Repository">
		                                    <Class endsWith="Repository" />
		                                </Layer>
		                            </ArchitecturalLevels>
		                            """;

		const string source = """
		                      public interface ICheeseRepository { }
		                      public class MenuController(ICheeseRepository cheeseRepository) { }
		                      """;

		var updatedXml = await AnalyzerTestHelper.ApplyAddToExceptionsCodeFixAsync(
			source,
			[
				("Architecture.anl", parentConfig),
				("SharedPizzeriaLayers.xml", sharedConfig),
			],
			ArchitecturalDiagnosticIds.IllegalLevelDependency,
			"SharedPizzeriaLayers.xml");

		var reparsed = XDocument.Parse(updatedXml);
		var exceptions = reparsed.Descendants("Class")
			.Single(c => c.Attribute("endsWith")?.Value == "Repository")
			.Element("Exceptions");

		exceptions.Should().NotBeNull();
		exceptions!.Elements("Class")
			.Where(c => c.Attribute("typeName")?.Value == "ICheeseRepository")
			.Should().ContainSingle();
	}

	private static Diagnostic CreateDiagnosticWithProperties(ImmutableDictionary<string, string?> properties)
    {
        return Diagnostic.Create(
            ArchitecturalDiagnostics.IllegalDependency,
            Location.None,
            properties,
            "PizzaController",
            "Controller",
            "CheeseRepository",
            "Repository");
    }
}
