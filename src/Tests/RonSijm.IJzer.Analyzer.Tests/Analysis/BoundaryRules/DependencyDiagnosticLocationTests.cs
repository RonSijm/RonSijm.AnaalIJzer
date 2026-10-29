using RonSijm.IJzer.Analyzer.Tests.Testing;
using RonSijm.IJzer.Core.Findings;
using RonSijm.IJzer.Core.Indicators;

namespace RonSijm.IJzer.Analyzer.Tests.Analysis.BoundaryRules;

public sealed class DependencyDiagnosticLocationTests
{
    private const string Config = """
                                  <ArchitecturalLevels>
                                    <Layer name="Caller"><Class exactName="CallerType" /></Layer>
                                    <Layer name="Dependency"><Class exactName="TargetDependency" /></Layer>
                                  </ArchitecturalLevels>
                                  """;

    public static TheoryData<string, string, string> SourceCases { get; } = new()
    {
        {
            DependencySites.Constructor,
            """
            public class TargetDependency { }
            public class CallerType
            {
                public CallerType(TargetDependency dependency) { }
            }
            """,
            "TargetDependency"
        },
        {
            DependencySites.Method,
            """
            public class TargetDependency { }
            public class CallerType
            {
                public void Use(TargetDependency dependency) { }
            }
            """,
            "TargetDependency"
        },
        {
            DependencySites.MethodReturn,
            """
            public class TargetDependency { }
            public class CallerType
            {
                public TargetDependency Get() => default!;
            }
            """,
            "TargetDependency"
        },
        {
            DependencySites.Field,
            """
            public class TargetDependency { }
            public class CallerType
            {
                private readonly TargetDependency dependency = default!;
            }
            """,
            "TargetDependency"
        },
        {
            DependencySites.Property,
            """
            public class TargetDependency { }
            public class CallerType
            {
                public TargetDependency Dependency { get; } = default!;
            }
            """,
            "TargetDependency"
        },
        {
            DependencySites.Local,
            """
            public class TargetDependency { }
            public class CallerType
            {
                public void Use()
                {
                    TargetDependency dependency = default!;
                }
            }
            """,
            "TargetDependency"
        },
        {
            DependencySites.Local,
            """
            public class TargetDependency { }
            public class DependencySource
            {
                public TargetDependency Get() => default!;
            }
            public class CallerType
            {
                public void Use(DependencySource source)
                {
                    var dependency = source.Get();
                }
            }
            """,
            "dependency"
        },
        {
            DependencySites.New,
            """
            public class TargetDependency { }
            public class CallerType
            {
                public object Create() => new TargetDependency();
            }
            """,
            "TargetDependency"
        },
        {
            DependencySites.GenericInvocation,
            """
            public class TargetDependency { }
            public sealed class Locator
            {
                public T Get<T>() => default!;
            }
            public class CallerType
            {
                public object Resolve(Locator locator) => locator.Get<TargetDependency>();
            }
            """,
            "TargetDependency"
        },
        {
            DependencySites.GenericArgument,
            """
            using System;
            public class TargetDependency { }
            public class CallerType
            {
                public CallerType(Lazy<TargetDependency> dependency) { }
            }
            """,
            "Lazy<TargetDependency>"
        },
        {
            DependencySites.Inheritance,
            """
            public class TargetDependency { }
            public class CallerType : TargetDependency { }
            """,
            "TargetDependency"
        },
        {
            DependencySites.InterfaceImplementation,
            """
            public interface TargetDependency { }
            public class CallerType : TargetDependency { }
            """,
            "TargetDependency"
        },
        {
            DependencySites.Attribute,
            """
            using System;
            public class TargetDependency : Attribute { }
            [TargetDependency]
            public class CallerType { }
            """,
            "TargetDependency"
        },
        {
            DependencySites.StaticMember,
            """
            public static class TargetDependency
            {
                public static void Use() { }
            }
            public class CallerType
            {
                public void Call() => TargetDependency.Use();
            }
            """,
            "TargetDependency"
        }
    };

    [Theory]
    [MemberData(nameof(SourceCases))]
    public async Task DependencyDiagnostic_UsesNonEmptySpanFromOffendingSyntaxTree(string site, string source, string expectedText)
    {
        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, Config);

        var diagnostic = diagnostics.Should().ContainSingle(item => item.Id == ArchitecturalDiagnosticIds.DependencyNotAllowed).Subject;
        diagnostic.Properties[ArchitecturalDiagnostics.PropertySite].Should().Be(site);
        diagnostic.Descriptor.CustomTags.Should().NotContain("CompilationEnd");
        diagnostic.Location.IsInSource.Should().BeTrue();
        diagnostic.Location.SourceTree.Should().NotBeNull();
        diagnostic.Location.SourceTree!.FilePath.Should().Be("Test.cs");
        diagnostic.Location.SourceSpan.Length.Should().BeGreaterThan(0);
        source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length).Should().Be(expectedText);
    }
}
