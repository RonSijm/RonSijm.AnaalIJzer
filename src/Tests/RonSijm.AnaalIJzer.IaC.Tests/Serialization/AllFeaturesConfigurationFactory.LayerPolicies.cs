namespace RonSijm.AnaalIJzer.IaC.Tests.Serialization;

internal static partial class AllFeaturesConfigurationFactory
{
    private static NameRules CreateNameRules()
    {
        var matchingNames = new RequireMatchingNames
        {
            ValueTracking = NameRuleValueTracking.IntraProcedural,
            AllowedSites = { DependencySite.Method },
            Name = new NameMatcher { EndsWith = "Id" },
            Source = new NameMatcher { StartsWith = "source" },
            Target = new NameMatcher { StartsWith = "target" },
            AllowedMappings =
            {
                new NameRuleAllow
                {
                    From = "legacyId",
                    To = "customerId",
                    Source = new NameMatcher { ExactName = "legacyId" },
                    Target = new NameMatcher { ExactName = "customerId" }
                }
            }
        };
        var declarationNames = new RequireDeclarationNameMatchesType
        {
            BlockedSites = { DependencySite.Field },
            Type = new NameMatcher { EndsWith = "Id" },
            Name = new NameMatcher { EndsWith = "Id" },
            AllowedMappings =
            {
                new DeclarationNameRuleAllow
                {
                    From = "PatientId",
                    To = "patientId",
                    Type = new NameMatcher { ExactName = "PatientId" },
                    Name = new NameMatcher { ExactName = "patientId" }
                }
            }
        };
        var result = new NameRules { Rules = { matchingNames, declarationNames } };

        return result;
    }

    private static ForbiddenOperations CreateForbiddenOperations()
    {
        var rule = new ForbiddenOperation { BlockedSites = { DependencySite.Constructor } };
        rule.Matchers.Add(CreateOperationMatcher(SemanticOperationKind.Invocation));
        var result = new ForbiddenOperations { Rules = { rule } };

        return result;
    }

    private static BehavioralOperations CreateBehavioralOperations()
    {
        var required = new RequiredOperation
        {
            DeclarationMatcher = CreateOperationDeclarationMatcher(),
            OperationMatchers = { CreateOperationMatcher(SemanticOperationKind.Invocation) }
        };
        var before = new RequiredOperationBefore
        {
            Ordering = BehavioralOperationOrdering.Lexical,
            DeclarationMatcher = CreateOperationDeclarationMatcher(),
            OperationMatchers = { CreateOperationMatcher(SemanticOperationKind.Invocation) },
            BeforeOperation = { Matchers = { CreateOperationMatcher(SemanticOperationKind.Return) } }
        };
        var after = new ForbiddenOperationAfter
        {
            DeclarationMatcher = CreateOperationDeclarationMatcher(),
            OperationMatchers = { CreateOperationMatcher(SemanticOperationKind.Invocation) },
            AfterOperation = { Matchers = { CreateOperationMatcher(SemanticOperationKind.Assignment) } }
        };
        var maximum = new MaximumOperationCount(2)
        {
            DeclarationMatcher = CreateOperationDeclarationMatcher(),
            OperationMatchers = { CreateOperationMatcher(SemanticOperationKind.Invocation) }
        };
        var result = new BehavioralOperations { Rules = { required, before, after, maximum } };

        return result;
    }

    private static OperationDeclarationMatcher CreateOperationDeclarationMatcher()
    {
        var result = new OperationDeclarationMatcher
        {
            ContainingType = new OperationContainingTypeMatcher { EndsWith = "Service" },
            Member = new OperationMemberMatcher
            {
                ExactName = "Execute",
                MemberKinds = { SemanticOperationMemberKind.Method }
            }
        };

        return result;
    }

    private static OperationMatcher CreateOperationMatcher(SemanticOperationKind kind)
    {
        var result = new OperationMatcher
        {
            Kinds = { kind },
            StaticAccess = false,
            ContainingType = new OperationContainingTypeMatcher { ExactName = "Audit" },
            Member = new OperationMemberMatcher
            {
                ExactName = "Record",
                MemberKinds = { SemanticOperationMemberKind.Method }
            }
        };

        return result;
    }

    private static ApiSurface CreateApiSurface()
    {
        var result = new ApiSurface
        {
            RequireRecognizedTypes = true,
            Rules =
            {
                new ApiSurfaceRule("Contracts") { AllowedSites = { DependencySite.MethodReturn } },
                new ApiSurfaceRule("/Infrastructure", false) { BlockedSites = { DependencySite.Constructor } }
            },
            TransitiveExposure = new TransitiveExposure { MaxDepth = 4 }
        };

        return result;
    }

    private static EntryPoints CreateEntryPoints()
    {
        var result = new EntryPoints
        {
            Items =
            {
                new EntryPoint { Layer = "Contracts", AllowedSites = { DependencySite.Method } },
                new EntryPoint
                {
                    Matchers =
                    {
                        new ClassMatcher { EndsWith = "Facade" },
                        new NamespaceMatcher { StartsWith = "Shop.Public" },
                        new AssemblyMatcher { EndsWith = ".Contracts" }
                    }
                }
            }
        };

        return result;
    }
}
