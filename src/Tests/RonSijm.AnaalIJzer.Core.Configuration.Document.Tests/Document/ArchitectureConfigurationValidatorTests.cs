using System.Xml.Linq;
using RonSijm.AnaalIJzer.Core.Configuration.Document.Validation;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Tests.Document;

public sealed class ArchitectureConfigurationValidatorTests
{
	[Fact]
	public void Validate_SupportsEveryDeclarationTarget()
	{
		var document = XDocument.Parse(
			"""
			<ArchitecturalLevels>
			  <Layer name="Requests">
			    <Class endsWith="Request">
			      <Type exactName="PizzaRequest" typeKind="Class" />
			      <NestedType exactName="PizzaMetadata" typeKind="Class" />
			      <Constructor exactName="PizzaRequest" typeName="PizzaRequest" />
			      <Method exactName="Project" typeName="PizzaProjection" />
			      <Property exactName="PizzaId" typeName="PizzaId" />
			      <Field exactName="_tenantId" typeName="TenantId" />
			      <Event exactName="Changed" typeName="Action" />
			      <Operator typeName="PizzaRequest" />
			      <Conversion typeName="PizzaId" />
			    </Class>
			  </Layer>
			</ArchitecturalLevels>
			""",
			LoadOptions.SetLineInfo);

		var result = ArchitectureConfigurationValidator.Validate(document, "Architecture.anl");

		result.Should().BeEmpty();
	}

	[Fact]
	public void Validate_ReportsMissingMatcherAttribute()
	{
		var document = XDocument.Parse(
			"""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class />
			  </Layer>
			</ArchitecturalLevels>
			""",
			LoadOptions.SetLineInfo);

		var result = ArchitectureConfigurationValidator.Validate(document, "Architecture.anl");

		result.Should().Contain(issue => issue.Message.Contains("Class requires at least one matcher attribute.", StringComparison.Ordinal));
	}

	[Fact]
	public void Validate_ReportsInvalidRegex()
	{
		var document = XDocument.Parse(
			"""
			<ArchitecturalLevels>
			  <Layer name="Application">
			    <Class regex="[" />
			  </Layer>
			</ArchitecturalLevels>
			""",
			LoadOptions.SetLineInfo);

		var result = ArchitectureConfigurationValidator.Validate(document, "Architecture.anl");

		result.Should().Contain(issue => issue.Message.Contains("Invalid regular expression", StringComparison.Ordinal));
	}

	[Fact]
	public void Validate_ClassMatcherDeclarationChildWithoutMatcher_ReportsIssue()
	{
		var document = XDocument.Parse(
			"""
			<ArchitecturalLevels>
			  <Layer name="Requests">
			    <Class endsWith="Request">
			      <Property />
			    </Class>
			  </Layer>
			</ArchitecturalLevels>
			""",
			LoadOptions.SetLineInfo);

		var result = ArchitectureConfigurationValidator.Validate(document, "Architecture.anl");

		result.Should().Contain(issue => issue.Message.Contains("Property requires at least one matcher attribute.", StringComparison.Ordinal));
	}

	[Fact]
	public void Validate_ClassMatcherDeclarationChildrenWithMatchers_RemainValid()
	{
		var document = XDocument.Parse(
			"""
			<ArchitecturalLevels>
			  <Layer name="Requests">
			    <Class endsWith="Request">
			      <Property exactName="PizzaId" typeName="PizzaId" />
			      <Field exactName="_tenantId" typeName="TenantId" />
			    </Class>
			  </Layer>
			</ArchitecturalLevels>
			""",
			LoadOptions.SetLineInfo);

		var result = ArchitectureConfigurationValidator.Validate(document, "Architecture.anl");

		result.Should().BeEmpty();
	}

	[Fact]
	public void Validate_ObservationMatchers_AllowExistenceWithoutAttributes()
	{
		var document = XDocument.Parse(
			"""
			<ArchitecturalLevels>
			  <Layer name="FallbackServices">
			    <Class endsWith="Service">
			      <Method exactName="PizzaDelivery">
			        <Throw />
			      </Method>
			    </Class>
			  </Layer>
			</ArchitecturalLevels>
			""",
			LoadOptions.SetLineInfo);

		var result = ArchitectureConfigurationValidator.Validate(document, "Architecture.anl");

		result.Should().BeEmpty();
	}

	[Fact]
	public void Validate_ObservationMatchers_RejectUnsupportedSemanticAttributes()
	{
		var document = XDocument.Parse(
			"""
			<ArchitecturalLevels>
			  <Layer name="FallbackServices">
			    <Class endsWith="Service">
			      <Method exactName="PizzaDelivery">
			        <Throw typeKind="Class" />
			      </Method>
			    </Class>
			  </Layer>
			</ArchitecturalLevels>
			""",
			LoadOptions.SetLineInfo);

		var result = ArchitectureConfigurationValidator.Validate(document, "Architecture.anl");

		result.Should().Contain(issue => issue.Message.Contains("Throw supports typeName, exactName, exactFullName, endsWith, startsWith, contains, or regex matchers.", StringComparison.Ordinal));
	}
}
