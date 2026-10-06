namespace RonSijm.AnaalIJzer.IaC.Tests.Serialization;

internal static partial class AllFeaturesConfigurationFactory
{
    private static OperationContracts CreateOperationContracts()
    {
        var owner = new OperationDeclarationSelector(new OperationDeclarationMatcher
        {
            ContainingType = new OperationContainingTypeMatcher { EndsWith = "Service" },
            Member = new OperationMemberMatcher
            {
                ExactName = "Execute",
                MemberKinds = { SemanticOperationMemberKind.Method }
            }
        });
        var operation = new OperationContract("OrderPizza", owner)
        {
            AllowedOwnerLayers = { "Application" },
            AllowedEntryPointLayers = { "Controller" },
            Request = new OperationTypeSelector(new ClassMatcher { EndsWith = "Request" }),
            Response = new OperationTypeSelector(new ClassMatcher { EndsWith = "Response" }),
            EntryPoints =
            {
                new OperationDeclarationSelector(new OperationDeclarationMatcher
                {
                    ContainingType = new OperationContainingTypeMatcher { EndsWith = "Controller" },
                    Member = new OperationMemberMatcher
                    {
                        ExactName = "Post",
                        MemberKinds = { SemanticOperationMemberKind.Method }
                    }
                })
            }
        };
        var result = new OperationContracts { Operations = { operation } };

        return result;
    }
}
