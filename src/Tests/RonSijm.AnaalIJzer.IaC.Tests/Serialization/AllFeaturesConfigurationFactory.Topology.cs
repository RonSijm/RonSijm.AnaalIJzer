namespace RonSijm.AnaalIJzer.IaC.Tests.Serialization;

internal static partial class AllFeaturesConfigurationFactory
{
    private static ProjectArchitecture CreateProjectArchitecture()
    {
        var domain = new ProjectGroup("Domain")
        {
            Projects = { new ProjectMatcher { EndsWith = ".Domain" } }
        };
        var application = new ProjectGroup("Application")
        {
            Projects = { new ProjectMatcher { EndsWith = ".Application" } }
        };
        var reference = new ProjectReferenceRule("Application", "Domain")
        {
            FromMatchers = { new ProjectMatcher { ExactName = "Shop.Application" } },
            ToMatchers = { new ProjectMatcher { ExactName = "Shop.Domain" } }
        };
        var blockedReference = new ProjectReferenceRule("Domain", "Application", false);
        var packages = new PackagePolicy("Application")
        {
            IncludeTransitive = true,
            RuleSets =
            {
                new PackageRuleSet(true) { Packages = { new PackageMatcher { StartsWith = "Microsoft.Extensions" } } },
                new PackageRuleSet(false) { Packages = { new PackageMatcher { ExactName = "Legacy.Package" } } }
            }
        };
        var assemblies = new AssemblyReferencePolicy("Domain")
        {
            RuleSets =
            {
                new AssemblyReferenceRuleSet(true) { Assemblies = { new AssemblyReferenceMatcher { StartsWith = "System" } } },
                new AssemblyReferenceRuleSet(false) { Assemblies = { new AssemblyReferenceMatcher { ExactName = "Legacy.Transport" } } }
            }
        };
        var result = new ProjectArchitecture
        {
            RequireRecognizedProjects = true,
            Elements = { domain, application, reference, blockedReference, packages, assemblies }
        };

        return result;
    }

    private static SolutionTopology CreateSolutionTopology()
    {
        var diningRoom = new SolutionModule("DiningRoom")
        {
            Projects = { new ProjectMatcher { EndsWith = ".Web" } }
        };
        var kitchen = new SolutionModule("Kitchen")
        {
            Projects = { new ProjectMatcher { EndsWith = ".Application" } }
        };
        var result = new SolutionTopology
        {
            RequireRecognizedProjects = true,
            EnforceAcyclic = true,
            Elements =
            {
                diningRoom,
                kitchen,
                new SolutionModuleReference("DiningRoom", "Kitchen"),
                new SolutionModuleReference("Kitchen", "DiningRoom", false)
            }
        };

        return result;
    }

    private static AssemblyAttributePolicy CreateAssemblyAttributePolicy()
    {
        var allowedAttribute = new AssemblyAttributeMatcher
        {
            ExactFullName = "System.Runtime.CompilerServices.InternalsVisibleToAttribute",
            Arguments = { new AssemblyAttributeArgumentMatcher { Index = 0, ExactName = "Approved.Tests" } }
        };
        var forbiddenAttribute = new AssemblyAttributeMatcher
        {
            WithAttribute = "ObsoleteAttribute",
            Arguments = { new AssemblyAttributeArgumentMatcher { Name = "Reason", Contains = "temporary" } }
        };
        var result = new AssemblyAttributePolicy
        {
            RuleSets =
            {
                new AssemblyAttributeRuleSet(true) { Attributes = { allowedAttribute } },
                new AssemblyAttributeRuleSet(false) { Attributes = { forbiddenAttribute } }
            }
        };

        return result;
    }

    private static ReturnValuePolicy CreateGlobalReturnValuePolicy()
    {
        var result = new ReturnValuePolicy();
        foreach (var kind in Enum.GetValues<ReturnValueKind>())
        {
            result.ForbiddenReturns.Add(new ReturnValueMatcher(kind) { ExactName = "Example" });
        }

        return result;
    }

    private static NamespaceHierarchyPolicy CreateNamespaceHierarchyPolicy()
    {
        var result = new NamespaceHierarchyPolicy("Shop");
        foreach (var relation in Enum.GetValues<NamespaceHierarchyRelation>())
        {
            result.BlockedRelations.Add(new NamespaceHierarchyBlockedRelation(relation) { AllowedSites = { DependencySite.Method } });
        }

        return result;
    }
}
