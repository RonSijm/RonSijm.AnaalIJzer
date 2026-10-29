namespace RonSijm.IJzer.Annotations;

internal static class ArchitectureInternalAttributeTargets
{
    internal const AttributeTargets All = AttributeTargets.Class
                                          | AttributeTargets.Struct
                                          | AttributeTargets.Interface
                                          | AttributeTargets.Enum
                                          | AttributeTargets.Delegate
                                          | AttributeTargets.Constructor
                                          | AttributeTargets.Method
                                          | AttributeTargets.Property
                                          | AttributeTargets.Field
                                          | AttributeTargets.Event;
}
