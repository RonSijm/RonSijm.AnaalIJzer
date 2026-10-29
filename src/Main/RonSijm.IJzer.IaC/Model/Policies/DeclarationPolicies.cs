namespace RonSijm.IJzer.IaC;

public sealed class VisibilityPolicy : ArchitectureElement, ILayerConfigurationElement
{
    public Collection<DeclarationTarget> Targets { get; } = [];
    public Collection<DeclaredAccessibility> AllowedAccessibilities { get; } = [];
    public Collection<DeclaredAccessibility> BlockedAccessibilities { get; } = [];

    internal override string ElementName => "VisibilityPolicy";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteList("targets", Targets);
        writer.WriteList("allowedAccessibilities", AllowedAccessibilities);
        writer.WriteList("blockedAccessibilities", BlockedAccessibilities);
    }
}

public sealed class InheritancePolicy : ArchitectureElement, ILayerConfigurationElement
{
    public Collection<DeclaredTypeKind> TypeKinds { get; } = [];
    public Collection<string> RequiredBaseTypes { get; } = [];
    public Collection<string> RequiredInterfaces { get; } = [];

    internal override string ElementName => "InheritancePolicy";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteList("typeKinds", TypeKinds);
        writer.WriteList("requiredBaseTypes", RequiredBaseTypes);
        writer.WriteList("requiredInterfaces", RequiredInterfaces);
    }
}

public sealed class ContractPolicy : ArchitectureElement, ILayerConfigurationElement
{
    public Collection<DeclaredTypeKind> AllowedTypeKinds { get; } = [];
    public Collection<ContractMemberKind> AllowedMemberKinds { get; } = [];
    public Collection<PropertyAccessor> AllowedPropertyAccessors { get; } = [];
    public bool AllowMethodBodies { get; set; }
    public bool AllowStaticMembers { get; set; }
    public bool AllowNestedTypes { get; set; }

    internal override string ElementName => "ContractPolicy";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteList("allowedTypeKinds", AllowedTypeKinds);
        writer.WriteList("allowedMemberKinds", AllowedMemberKinds);
        writer.WriteList("allowedPropertyAccessors", AllowedPropertyAccessors);
        writer.WriteWhenTrue("allowMethodBodies", AllowMethodBodies);
        writer.WriteWhenTrue("allowStaticMembers", AllowStaticMembers);
        writer.WriteWhenTrue("allowNestedTypes", AllowNestedTypes);
    }
}

public sealed class ApiSurface : ArchitectureElement, ILayerConfigurationElement
{
    public bool RequireRecognizedTypes { get; set; }
    public Collection<ApiSurfaceRule> Rules { get; } = [];
    public TransitiveExposure? TransitiveExposure { get; set; }

    internal override string ElementName => "ApiSurface";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteWhenTrue("requireRecognizedTypes", RequireRecognizedTypes);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        foreach (var rule in Rules)
        {
            yield return rule;
        }

        if (TransitiveExposure is not null)
        {
            yield return TransitiveExposure;
        }
    }
}

public sealed class ApiSurfaceRule(string path, bool allowed = true) : SiteFilteredElement
{
    public string Path { get; set; } = path;
    public bool Allowed { get; set; } = allowed;

    internal override string ElementName => Allowed ? "AllowedLayer" : "BlockedLayer";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteRequired("path", Path);
    }
}

public sealed class TransitiveExposure : ArchitectureElement
{
    public int? MaxDepth { get; set; }

    internal override string ElementName => "TransitiveExposure";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteOptional("maxDepth", MaxDepth);
    }
}

public sealed class SourceLocations : ArchitectureElement, ILayerConfigurationElement
{
    public SourceLocationBase RelativeTo { get; set; } = SourceLocationBase.Project;
    public Collection<SourceLocationMatcher> Sources { get; } = [];

    internal override string ElementName => "SourceLocations";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        if (RelativeTo != SourceLocationBase.Project)
        {
            writer.WriteValue("relativeTo", RelativeTo);
        }
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Sources.Cast<ArchitectureElement>();

        return result;
    }
}

public sealed class EntryPoints : ArchitectureElement, ILayerConfigurationElement
{
    public Collection<EntryPoint> Items { get; } = [];

    internal override string ElementName => "EntryPoints";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Items.Cast<ArchitectureElement>();

        return result;
    }
}

public sealed class EntryPoint : SiteFilteredElement
{
    public string? Layer { get; set; }
    public Collection<ArchitectureElement> Matchers { get; } = [];

    internal override string ElementName => "EntryPoint";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteOptional("layer", Layer);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Matchers;

        return result;
    }
}

public sealed class GeneratedCode(GeneratedCodeMode mode) : ArchitectureElement, IRootConfigurationElement
{
    public GeneratedCodeMode Mode { get; set; } = mode;
    public int? MaximumDocumentLength { get; set; }
    public Collection<GeneratedCodePathMatcher> Paths { get; } = [];

    internal override string ElementName => "GeneratedCode";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteValue("mode", Mode);
        writer.WriteOptional("maximumDocumentLength", MaximumDocumentLength);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Paths.Cast<ArchitectureElement>();

        return result;
    }
}
