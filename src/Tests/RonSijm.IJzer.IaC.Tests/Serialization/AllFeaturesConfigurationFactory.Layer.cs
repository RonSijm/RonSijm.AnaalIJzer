namespace RonSijm.IJzer.IaC.Tests.Serialization;

internal static partial class AllFeaturesConfigurationFactory
{
    private static Layer CreateLayer()
    {
        var result = new Layer("Application")
        {
            Description = "Every layer-scoped element is exercised here.",
            RequireRecognizedDependencies = { DependencySite.Method },
            Elements =
            {
                CreateStructuralClassMatcher(),
                new NamespaceMatcher { StartsWith = "Shop.Application", Exceptions = CreateExceptions() },
                new AssemblyMatcher { EndsWith = ".Application", Exceptions = CreateExceptions() },
                new Layer("Contracts")
                {
                    Elements = { new ClassMatcher { TypeKind = DeclaredTypeKind.Interface } }
                },
                CreateLayerAllowedTypes(),
                CreateLayerForbiddenTypes(),
                CreateNameRules(),
                CreateInheritancePolicy(),
                CreateVisibilityPolicy(),
                CreateContractPolicy(),
                CreateLayerReturnValuePolicy(),
                CreateForbiddenOperations(),
                CreateBehavioralOperations(),
                CreateApiSurface(),
                CreateEntryPoints(),
                CreateSourceLocations(),
                new AllowedDependency("Application", "Contracts") { AllowedSites = { DependencySite.InterfaceImplementation } },
                new BlockedDependency("Contracts", "Application") { AppliesToDescendants = true }
            }
        };

        return result;
    }

    private static ClassMatcher CreateStructuralClassMatcher()
    {
        var result = new ClassMatcher
        {
            EndsWith = "Service",
            TypeKind = DeclaredTypeKind.Class,
            Exceptions = CreateExceptions()
        };

        foreach (var target in Enum.GetValues<DeclarationTarget>())
        {
            var declaration = new DeclarationMatcher(target) { ExactName = "Example" };
            foreach (var observationKind in Enum.GetValues<CodeObservationKind>())
            {
                declaration.Observations.Add(new CodeObservationMatcher(observationKind) { ExactName = "Observed" });
            }

            result.Declarations.Add(declaration);
        }

        return result;
    }

    private static Exceptions CreateExceptions()
    {
        var nested = new Exceptions
        {
            Matchers =
            {
                new ClassException
                {
                    Reason = "Temporary exception",
                    Owner = "Architecture team",
                    ExpiresOn = new DateTime(2030, 1, 1),
                    Matcher = { ExactName = "LegacyService" }
                },
                new NamespaceException { Matcher = { Contains = ".Legacy" } },
                new AssemblyException { Matcher = { EndsWith = ".Legacy" } }
            }
        };

        return nested;
    }

    private static AllowedTypes CreateLayerAllowedTypes()
    {
        var result = new AllowedTypes
        {
            Matchers =
            {
                new ClassMatcher { EndsWith = "Service" },
                new NamespaceMatcher { StartsWith = "Shop" }
            }
        };

        return result;
    }

    private static ForbiddenTypes CreateLayerForbiddenTypes()
    {
        var result = new ForbiddenTypes
        {
            Matchers =
            {
                new ForbiddenClassMatcher { EndsWith = "Helper" },
                new ForbiddenNamespaceMatcher { Contains = "Obsolete" }
            }
        };

        return result;
    }

    private static InheritancePolicy CreateInheritancePolicy()
    {
        var result = new InheritancePolicy
        {
            TypeKinds = { DeclaredTypeKind.Class },
            RequiredBaseTypes = { "Entity" },
            RequiredInterfaces = { "IEntity" }
        };

        return result;
    }

    private static VisibilityPolicy CreateVisibilityPolicy()
    {
        var result = new VisibilityPolicy
        {
            Targets = { DeclarationTarget.Type, DeclarationTarget.Method },
            AllowedAccessibilities = { DeclaredAccessibility.Public, DeclaredAccessibility.Internal }
        };

        return result;
    }

    private static ContractPolicy CreateContractPolicy()
    {
        var result = new ContractPolicy
        {
            AllowedTypeKinds = { DeclaredTypeKind.Interface },
            AllowedMemberKinds = { ContractMemberKind.Method, ContractMemberKind.Property },
            AllowedPropertyAccessors = { PropertyAccessor.Get },
            AllowMethodBodies = true,
            AllowStaticMembers = true,
            AllowNestedTypes = true
        };

        return result;
    }

    private static ReturnValuePolicy CreateLayerReturnValuePolicy()
    {
        var result = new ReturnValuePolicy
        {
            ForbiddenReturns = { new ReturnValueMatcher(ReturnValueKind.Literal) { Value = "null" } },
            AllowedReturns = new AllowedReturns
            {
                Matchers = { new ReturnValueMatcher(ReturnValueKind.Identifier) }
            }
        };

        return result;
    }

    private static SourceLocations CreateSourceLocations()
    {
        var result = new SourceLocations
        {
            RelativeTo = SourceLocationBase.Configuration,
            Sources = { new SourceLocationMatcher { StartsWith = "Application/", AssemblyName = "Shop.Application" } }
        };

        return result;
    }
}
