using System.Xml.Linq;
using RonSijm.AnaalIJzer.Config.Parsing;
using RonSijm.AnaalIJzer.ConfigurationEditing.Document;

namespace RonSijm.AnaalIJzer.Core.Configuration.Document.Tests.Document;

public sealed class ArchitectureConfigurationValidatorTests
{
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
}
