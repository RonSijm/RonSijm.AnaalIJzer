using RonSijm.AnaalIJzer.UnitTests.TestSupport;

namespace RonSijm.AnaalIJzer.UnitTests.Matching;

public sealed class CombinedMatcherTests
{
	public static TheoryData<string, string> DeclaredTypeKinds
    {
        get
        {
            return new()
            {
                { "Class", "public class Target { }" },
                { "Interface", "public interface Target { }" },
                { "Struct", "public struct Target { }" },
                { "Record", "public record Target;" },
                { "RecordStruct", "public record struct Target;" },
                { "Enum", "public enum Target { Value }" },
                { "Delegate", "public delegate void Target();" },
                { "iNtErFaCe", "public interface Target { }" }
            };
        }
    }

    [Theory]
	[MemberData(nameof(DeclaredTypeKinds))]
	public async Task TypeKind_MatchesEveryDeclaredKind(string typeKind, string declaration)
	{
		var config = $$"""
		               <ArchitecturalLevels>
		                 <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                 <Layer name="Target"><Class typeKind="{{typeKind}}" /></Layer>
		                 <AllowedDependency from="Caller" to="Target" />
		               </ArchitecturalLevels>
		               """;
		var source = $$"""
		               {{declaration}}
		               public class CallerType(Target dependency) { }
		               """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Theory]
	[InlineData("Class", "public record Target;")]
	[InlineData("Struct", "public record struct Target;")]
	[InlineData("Record", "public class Target { }")]
	[InlineData("RecordStruct", "public struct Target { }")]
	public async Task TypeKind_SeparatesRecordsFromClassesAndStructs(string typeKind, string declaration)
	{
		var config = $$"""
		               <ArchitecturalLevels requireRecognizedDependencies="Constructor">
		                 <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                 <Layer name="Target"><Class typeName="Target" typeKind="{{typeKind}}" /></Layer>
		               </ArchitecturalLevels>
		               """;
		var source = $$"""
		               {{declaration}}
		               public class CallerType(Target dependency) { }
		               """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency);
	}

	[Fact]
	public async Task UnknownTypeKind_ReportsARCH006()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Target"><Class typeKind="Component" /></Layer>
		                      </ArchitecturalLevels>
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class Target { }", config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration);
	}

	[Theory]
	[InlineData("Namespace")]
	[InlineData("Assembly")]
	public async Task TypeKind_OnNonClassMatcherReportsARCH006(string elementName)
	{
		var config = $$"""
		               <ArchitecturalLevels>
		                 <Layer name="Target"><{{elementName}} startsWith="Test" typeKind="Class" /></Layer>
		               </ArchitecturalLevels>
		               """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync("public class Target { }", config);

		diagnostics.Should().Contain(item => item.Id == ArchitecturalDiagnosticIds.InvalidConfiguration);
	}

	[Fact]
	public async Task ClassConditions_AreConjunctiveAndSiblingElementsAreAlternatives()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                        <Layer name="Contracts">
		                          <Class startsWith="I" endsWith="Repository" typeKind="Interface" />
		                          <Class startsWith="I" endsWith="Gateway" typeKind="Interface" />
		                        </Layer>
		                        <AllowedDependency from="Caller" to="Contracts" />
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public interface IExampleRepository { }
		                      public interface IExampleGateway { }
		                      public class CallerType(IExampleRepository repository, IExampleGateway gateway) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task FailedClassCondition_DoesNotMatch()
	{
		const string config = """
		                      <ArchitecturalLevels requireRecognizedDependencies="Constructor">
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                        <Layer name="Contracts"><Class endsWith="Repository" typeKind="Interface" /></Layer>
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public class ExampleRepository { }
		                      public class CallerType(ExampleRepository repository) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.UnrecognizedDependency);
	}

	[Fact]
	public async Task NamespaceConditions_AreConjunctive()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                        <Layer name="Data"><Namespace startsWith="Company." endsWith=".Data" /></Layer>
		                        <AllowedDependency from="Caller" to="Data" />
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      namespace Company.Orders.Data { public class OrderRecord { } }
		                      public class CallerType(Company.Orders.Data.OrderRecord record) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task AssemblyConditions_AreConjunctive()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Caller"><Assembly startsWith="Test" endsWith="Assembly" /></Layer>
		                        <Layer name="Dependency"><Class typeName="TargetDependency" /></Layer>
		                        <AllowedDependency from="Caller" to="Dependency" />
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public class TargetDependency { }
		                      public class CallerType(TargetDependency dependency) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task ExactCompositeThatFails_FallsThroughToPatternMatcher()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                        <Layer name="Wrong"><Class typeName="Target" typeKind="Interface" /></Layer>
		                        <Layer name="Correct"><Class endsWith="Target" typeKind="Class" /></Layer>
		                        <AllowedDependency from="Caller" to="Correct" />
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public class Target { }
		                      public class CallerType(Target dependency) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		diagnostics.Should().BeEmpty();
	}

	[Fact]
	public async Task CombinedMatchers_ApplyToAllowedAndForbiddenPolicies()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Allowed><Class endsWith="Repository" typeKind="Interface" /></Allowed>
		                        <Forbidden><Class startsWith="Legacy" endsWith="Repository" typeKind="Class" /></Forbidden>
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                        <Layer name="Data"><Class endsWith="Repository" /></Layer>
		                        <AllowedDependency from="Caller" to="Data" />
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public interface IExampleRepository { }
		                      public class ExampleRepository { }
		                      public class LegacyExampleRepository { }
		                      public class CallerType(IExampleRepository allowed, ExampleRepository notAllowed, LegacyExampleRepository forbidden) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violations = diagnostics.Where(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenDependency).ToArray();
		violations.Should().HaveCount(2);
	}

	[Fact]
	public async Task CombinedMatcher_AppliesToNestedExceptions()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Forbidden>
		                          <Class endsWith="Repository" typeKind="Interface">
		                            <Exceptions>
		                              <Class startsWith="ILegacy" endsWith="Repository" typeKind="Interface" />
		                            </Exceptions>
		                          </Class>
		                        </Forbidden>
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public interface ILegacyRepository { }
		                      public interface INewRepository { }
		                      public class CallerType(ILegacyRepository allowed, INewRepository forbidden) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenDependency).Which;
		violation.GetMessage(CultureInfo.InvariantCulture).Should().Contain("INewRepository");
	}

	[Fact]
	public async Task CombinedEndsWithMatcher_PreservesRenameMetadata()
	{
		const string config = """
		                      <ArchitecturalLevels>
		                        <Forbidden>
		                          <Class endsWith="Store" typeKind="Class">
		                            <Fix Rename="Repository" />
		                          </Class>
		                        </Forbidden>
		                        <Layer name="Caller"><Class typeName="CallerType" /></Layer>
		                      </ArchitecturalLevels>
		                      """;
		const string source = """
		                      public class CheeseStore { }
		                      public class CallerType(CheeseStore store) { }
		                      """;

		var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

		var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.ForbiddenDependency).Which;
		violation.Properties[ArchitecturalLevelAnalyzer.PropertyMatchedSuffix].Should().Be("Store");
		violation.Properties[ArchitecturalLevelAnalyzer.PropertyFixSuffix].Should().Be("Repository");

		var fixedSource = await AnalyzerTestHelper.ApplyCodeFixAsync(source, config);
		fixedSource.Should().Contain("class CheeseRepository");
	}
}
