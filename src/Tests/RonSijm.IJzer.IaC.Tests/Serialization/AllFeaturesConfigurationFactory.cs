namespace RonSijm.IJzer.IaC.Tests.Serialization;

internal static partial class AllFeaturesConfigurationFactory
{
    public static ArchitectureConfiguration Create()
    {
        var result = new ArchitectureConfiguration
        {
            Description = "An exhaustive IaC serialization fixture.",
            EnforceAcyclic = true,
            EnforceObservedAcyclic = true,
            EnableReport = true,
            ReportPath = "violations.md",
            EnableDocumentation = true,
            DocumentationPath = "architecture.md",
            RequireRecognizedDependencies = { DependencySite.Constructor },
            Elements =
            {
                new Include("Optional/*.anl") { AllowNoMatches = true },
                new ExceptionPolicy { RequireReason = true, RequireOwner = true, RequireExpiresOn = true, WarnBeforeDays = 7 },
                CreateLayer(),
                CreateRootAllowedTypes(),
                CreateRootForbiddenTypes(),
                new AllowedDependency("*", "Application") { AppliesToDescendants = true },
                new BlockedDependency("Application", "Application") { BlockedSites = { DependencySite.Field } },
                CreateProjectArchitecture(),
                CreateSolutionTopology(),
                CreateOperationContracts(),
                CreateAssemblyAttributePolicy(),
                CreateGlobalReturnValuePolicy(),
                CreateNamespaceHierarchyPolicy(),
                new GeneratedCode(GeneratedCodeMode.IncludeConfigured)
                {
                    MaximumDocumentLength = 100000,
                    Paths = { new GeneratedCodePathMatcher { EndsWith = ".g.cs" } }
                }
            }
        };

        return result;
    }

    private static AllowedTypes CreateRootAllowedTypes()
    {
        var result = new AllowedTypes
        {
            Matchers =
            {
                new ClassMatcher { StartsWith = "I", TypeKind = DeclaredTypeKind.Interface },
                new NamespaceMatcher { StartsWith = "System" }
            }
        };

        return result;
    }

    private static ForbiddenTypes CreateRootForbiddenTypes()
    {
        var result = new ForbiddenTypes
        {
            Matchers =
            {
                new ForbiddenClassMatcher
                {
                    EndsWith = "Store",
                    Fix = new RenameFix("Repository")
                },
                new ForbiddenNamespaceMatcher { Contains = "Legacy" }
            }
        };

        return result;
    }
}
