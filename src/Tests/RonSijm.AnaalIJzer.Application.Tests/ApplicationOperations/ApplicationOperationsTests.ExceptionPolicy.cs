using System.Globalization;

namespace RonSijm.AnaalIJzer.Application.Tests.ApplicationOperations;

public sealed partial class ApplicationOperationsTests
{
	[Fact]
	public async Task ApplicationRunner_Inspect_ReturnsStructuredArch017FindingsWithStates()
	{
		var cancellationToken = TestContext.Current.CancellationToken;
		var tempDirectory = CreateRepositoryTempDirectory("AnaalIJzer-inspect-exception-policy");

		try
		{
			var projectPath = Path.Combine(tempDirectory, "Example.csproj");
			var sourcePath = Path.Combine(tempDirectory, "Kitchen.cs");
			var configPath = Path.Combine(tempDirectory, "Architecture.anl");
			var today = ArchitectureClock.UtcToday;
			var expiringSoonDate = today.AddDays(2).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
			var expiredDate = today.AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
			var futureDate = today.AddDays(30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
			await File.WriteAllTextAsync(projectPath, """
			                                          <Project Sdk="Microsoft.NET.Sdk">
			                                            <PropertyGroup>
			                                              <TargetFramework>net10.0</TargetFramework>
			                                              <Nullable>enable</Nullable>
			                                            </PropertyGroup>
			                                            <ItemGroup>
			                                              <AdditionalFiles Include="Architecture.anl" />
			                                            </ItemGroup>
			                                          </Project>
			                                          """, cancellationToken);
			await File.WriteAllTextAsync(sourcePath, """
			                                         public class MissingMetadataKitchen { }
			                                         public class SoonKitchen { }
			                                         public class ExpiredKitchen { }
			                                         """, cancellationToken);
			await File.WriteAllTextAsync(
				configPath,
				$"""
				<ArchitecturalLevels>
				  <ExceptionPolicy requireReason="true" requireOwner="true" requireExpiresOn="true" warnBeforeDays="14" />
				  <Layer name="Kitchen">
				    <Class endsWith="Kitchen">
				      <Exceptions>
				        <Class typeName="MissingMetadataKitchen" />
				        <Class typeName="SoonKitchen" reason="Temporary exception" owner="Kitchen team" expiresOn="{expiringSoonDate}" />
				        <Class typeName="ExpiredKitchen" reason="Expired exception" owner="Kitchen team" expiresOn="{expiredDate}" />
				        <Class typeName="GhostKitchen" reason="Ghost exception" owner="Kitchen team" expiresOn="{futureDate}" />
				      </Exceptions>
				    </Class>
				  </Layer>
				</ArchitecturalLevels>
				""",
				cancellationToken);

			var result = await new ApplicationRunner().ExecuteAsync(new ApplicationRequest(ApplicationOperationKind.Inspect)
			{
				InputKind = ApplicationInputKind.Project,
				InputPaths = [projectPath],
				WriteOutput = false
			}, cancellationToken);
			var exceptionFindings = result.Findings.Where(finding => finding.Category == "ARCH017").ToArray();

			exceptionFindings.Should().NotBeEmpty();
			exceptionFindings.Select(finding => finding.State).Should().Contain(["Invalid", "ExpiringSoon", "Expired", "Stale"]);
			exceptionFindings.Should().Contain(finding => finding.State == "Stale" && finding.Message.Contains("GhostKitchen", StringComparison.Ordinal));
			result.Content.Should().Contain("ARCH017");
		}
		finally
		{
			Directory.Delete(tempDirectory, recursive: true);
		}
	}
}

