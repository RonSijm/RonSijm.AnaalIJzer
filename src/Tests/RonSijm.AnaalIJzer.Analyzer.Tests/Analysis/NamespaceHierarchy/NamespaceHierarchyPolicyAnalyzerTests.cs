using RonSijm.AnaalIJzer.Analyzer.Tests.Testing;
using RonSijm.AnaalIJzer.Core.Findings;

namespace RonSijm.AnaalIJzer.Analyzer.Tests.Analysis.NamespaceHierarchy;

public sealed class NamespaceHierarchyPolicyAnalyzerTests
{
    public static TheoryData<string> DependencySites { get; } =
    [
        "Constructor",
        "Method",
        "MethodReturn",
        "Field",
        "Property",
        "Local",
        "New",
        "GenericInvocation",
        "GenericArgument",
        "Inheritance",
        "InterfaceImplementation",
        "Attribute",
        "StaticMember"
    ];

    [Theory]
    [MemberData(nameof(DependencySites))]
    public async Task NamespaceHierarchyPolicy_BlocksDescendantToAncestorAtEveryDependencySite(string site)
    {
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(CreateSource(site), CreateConfig());

        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement).Subject;
        violation.Properties[ArchitecturalDiagnostics.PropertyNamespaceHierarchyRelation].Should().Be("DescendantToAncestor");
        violation.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(site);
        violation.Properties[ArchitecturalDiagnostics.PropertyCallerNamespace].Should().Be("Shop.Requests");
        violation.Properties[ArchitecturalDiagnostics.PropertyDependencyNamespace].Should().Be("Shop");
    }

    [Fact]
    public async Task NamespaceHierarchyPolicy_UsesSiteFiltersToScopeABlockedRelation()
    {
        const string source = """
			namespace Shop
			{
				public sealed class SharedThing { }
			}

			namespace Shop.Requests
			{
				public sealed class Request
				{
					private Shop.SharedThing? field;

					public void Set(Shop.SharedThing value) { }
				}
			}
			""";
        const string config = """
			<ArchitecturalLevels>
			  <NamespaceHierarchyPolicy rootNamespace="Shop">
			    <BlockedRelation relation="DescendantToAncestor" allowedSites="Field" />
			  </NamespaceHierarchyPolicy>
			</ArchitecturalLevels>
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement).Subject;
        violation.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be("Field");
    }

    [Fact]
    public async Task NamespaceHierarchyPolicy_UsesBlockedSitesToExcludeTheNamedSite()
    {
        const string source = """
			namespace Shop
			{
				public sealed class SharedThing { }
			}

			namespace Shop.Requests
			{
				public sealed class Request
				{
					private Shop.SharedThing? field;

					public void Set(Shop.SharedThing value) { }
				}
			}
			""";
        const string config = """
			<ArchitecturalLevels>
			  <NamespaceHierarchyPolicy rootNamespace="Shop">
			    <BlockedRelation relation="DescendantToAncestor" blockedSites="Field" />
			  </NamespaceHierarchyPolicy>
			</ArchitecturalLevels>
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement).Subject;
        violation.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be("Method");
    }

    [Fact]
    public async Task NamespaceHierarchyPolicy_SuppressesALayerDiagnosticForTheSameReference()
    {
        const string source = """
			namespace Shop
			{
				public sealed class SharedThing { }
			}

			namespace Shop.Requests
			{
				public sealed class Request(Shop.SharedThing value) { }
			}
			""";
        const string config = """
			<ArchitecturalLevels>
			  <NamespaceHierarchyPolicy rootNamespace="Shop">
			    <BlockedRelation relation="DescendantToAncestor" />
			  </NamespaceHierarchyPolicy>
			  <Layer name="Root">
			    <Namespace exactName="Shop" />
			  </Layer>
			  <Layer name="Requests">
			    <Namespace exactName="Shop.Requests" />
			  </Layer>
			</ArchitecturalLevels>
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

        diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement);
        diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.DependencyNotAllowed);
    }

    [Fact]
    public async Task NamespaceHierarchyPolicy_LeavesLayerDiagnosticsInPlaceWhenItDoesNotBlockTheReference()
    {
        const string source = """
			namespace Shop
			{
				public sealed class SharedThing { }
			}

			namespace Shop.Requests
			{
				public sealed class Request(Shop.SharedThing value) { }
			}
			""";
        const string config = """
			<ArchitecturalLevels>
			  <NamespaceHierarchyPolicy rootNamespace="Shop">
			    <BlockedRelation relation="SameNamespace" />
			  </NamespaceHierarchyPolicy>
			  <Layer name="Root">
			    <Namespace exactName="Shop" />
			  </Layer>
			  <Layer name="Requests">
			    <Namespace exactName="Shop.Requests" />
			  </Layer>
			</ArchitecturalLevels>
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

        diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement);
        diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.DependencyNotAllowed);
    }

    [Fact]
    public async Task NamespaceHierarchyPolicy_UsesTheContainingNamespaceForNestedTypes()
    {
        const string source = """
			namespace Shop
			{
				public sealed class SharedThing { }
			}

			namespace Shop.Requests
			{
				public sealed class Request
				{
					public sealed class Details(Shop.SharedThing value) { }
				}
			}
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig());

        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement).Subject;
        violation.Properties[ArchitecturalDiagnostics.PropertyCallerNamespace].Should().Be("Shop.Requests");
    }

    [Fact]
    public async Task NamespaceHierarchyPolicy_ReportsInvalidNestedPolicyAsConfigurationError()
    {
        const string source = """
			namespace Shop.Requests
			{
				public sealed class Request { }
			}
			""";
        const string config = """
			<ArchitecturalLevels>
			  <Layer name="Requests">
			    <Namespace exactName="Shop.Requests" />
			    <NamespaceHierarchyPolicy rootNamespace="Shop">
			      <BlockedRelation relation="DescendantToAncestor" />
			    </NamespaceHierarchyPolicy>
			  </Layer>
			</ArchitecturalLevels>
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

        diagnostics.Should().Contain(item => item.Id == ArchitecturalDiagnosticIds.ConfigurationInvalid);
    }

    [Fact]
    public async Task NamespaceHierarchyPolicy_DoesNotTreatAUsingDirectiveAsADependency()
    {
        const string source = """
			using Shop;

			namespace Shop
			{
				public sealed class SharedThing { }
			}

			namespace Shop.Requests
			{
				public sealed class Request { }
			}
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig());

        diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement);
    }

    [Fact]
    public async Task NamespaceHierarchyPolicy_LeavesSimilarNamespacePrefixesAlone()
    {
        const string source = """
			namespace Shop.Requests
			{
				public sealed class SharedThing { }
			}

			namespace Shop.RequestsArchive
			{
				public sealed class ArchiveRequest(Shop.Requests.SharedThing value) { }
			}
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, CreateConfig());

        diagnostics.Should().NotContain(item => item.Id == ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement);
    }

    [Fact]
    public async Task NamespaceHierarchyPolicy_UsesTheFirstMatchingPolicyInDocumentOrder()
    {
        const string source = """
			namespace Shop.Requests
			{
				public sealed class SharedThing { }
			}

			namespace Shop.Requests.Child
			{
				public sealed class Request(Shop.Requests.SharedThing value) { }
			}
			""";
        const string config = """
			<ArchitecturalLevels>
			  <NamespaceHierarchyPolicy rootNamespace="Shop">
			    <BlockedRelation relation="DescendantToAncestor" />
			  </NamespaceHierarchyPolicy>
			  <NamespaceHierarchyPolicy rootNamespace="Shop.Requests">
			    <BlockedRelation relation="DescendantToAncestor" />
			  </NamespaceHierarchyPolicy>
			</ArchitecturalLevels>
			""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, config);

        var violation = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement).Subject;
        violation.Properties[ArchitecturalDiagnostics.PropertyNamespaceHierarchyRoot].Should().Be("Shop");
    }

    [Fact]
    public async Task NamespaceHierarchyPolicy_ReadsInlineAssemblyMetadata()
    {
        const string source = """"
			using System.Reflection;

			[assembly: AssemblyMetadata("AnaalIJzerSettings", """
			<ArchitecturalLevels>
			  <NamespaceHierarchyPolicy rootNamespace="Shop">
			    <BlockedRelation relation="DescendantToAncestor" />
			  </NamespaceHierarchyPolicy>
			</ArchitecturalLevels>
			""")]

			namespace Shop
			{
				public sealed class SharedThing { }
			}

			namespace Shop.Requests
			{
				public sealed class Request(Shop.SharedThing value) { }
			}
			"""";

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source);

        diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.NamespaceBoundaryPlacement);
    }

    private static string CreateConfig()
    {
        var result = """
			<ArchitecturalLevels>
			  <NamespaceHierarchyPolicy rootNamespace="Shop">
			    <BlockedRelation relation="DescendantToAncestor" description="Feature namespaces own their implementation details." />
			  </NamespaceHierarchyPolicy>
			</ArchitecturalLevels>
			""";

        return result;
    }

    private static string CreateSource(string site)
    {
        var member = site switch
        {
            "Constructor" => "public sealed class Request(Shop.SharedThing value) { }",
            "Method" => "public sealed class Request { public void Set(Shop.SharedThing value) { } }",
            "MethodReturn" => "public sealed class Request { public Shop.SharedThing Get() => null!; }",
            "Field" => "public sealed class Request { private Shop.SharedThing? value; }",
            "Property" => "public sealed class Request { public Shop.SharedThing? Value { get; } }",
            "Local" => "public sealed class Request { public void Run() { Shop.SharedThing value = null!; } }",
            "New" => "public sealed class Request { public void Run() { new Shop.SharedThing(); } }",
            "GenericInvocation" => "public sealed class Request { public void Run() { Create<Shop.SharedThing>(); } private static void Create<T>() { } }",
            "GenericArgument" => "public sealed class Request { private System.Collections.Generic.List<Shop.SharedThing>? values; }",
            "Inheritance" => "public sealed class Request : Shop.SharedBase { }",
            "InterfaceImplementation" => "public sealed class Request : Shop.ISharedContract { }",
            "Attribute" => "[Shop.Shared] public sealed class Request { }",
            "StaticMember" => "public sealed class Request { public void Run() { Shop.SharedStatics.Run(); } }",
            _ => throw new ArgumentOutOfRangeException(nameof(site), site, "Unknown dependency site.")
        };
        var result = """
			namespace Shop
			{
				public sealed class SharedThing { }
				public class SharedBase { }
				public interface ISharedContract { }
				public sealed class SharedAttribute : System.Attribute { }
				public static class SharedStatics { public static void Run() { } }
			}

			namespace Shop.Requests
			{
			""" + member + """
			}
			""";

        return result;
    }
}