using Microsoft.CodeAnalysis;

namespace RonSijm.IJzer.Core.Visibility.Annotations;

public readonly struct ArchitectureInternalRestriction(ArchitectureInternalScope scope, ISymbol annotatedSymbol, INamedTypeSymbol? ownerType, IAssemblySymbol ownerAssembly, string attributeTypeName)
{
    public ArchitectureInternalScope Scope { get; } = scope;
    public ISymbol AnnotatedSymbol { get; } = annotatedSymbol;
    public INamedTypeSymbol? OwnerType { get; } = ownerType;
    public IAssemblySymbol OwnerAssembly { get; } = ownerAssembly;
    public string AttributeTypeName { get; } = attributeTypeName;

    public string AttributeDisplayName
    {
        get
        {
            var result = AttributeTypeName.Substring(AttributeTypeName.LastIndexOf('.') + 1);

            return result;
        }
    }
}
