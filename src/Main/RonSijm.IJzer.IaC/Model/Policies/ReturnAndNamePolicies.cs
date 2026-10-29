namespace RonSijm.IJzer.IaC;

public sealed class ReturnValuePolicy : ArchitectureElement, IRootConfigurationElement, ILayerConfigurationElement
{
    public Collection<ReturnValueMatcher> ForbiddenReturns { get; } = [];
    public AllowedReturns? AllowedReturns { get; set; }

    internal override string ElementName => "ReturnValuePolicy";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        foreach (var matcher in ForbiddenReturns)
        {
            yield return matcher;
        }

        if (AllowedReturns is not null)
        {
            yield return AllowedReturns;
        }
    }
}

public sealed class AllowedReturns : ArchitectureElement
{
    public Collection<ReturnValueMatcher> Matchers { get; } = [];

    internal override string ElementName => "AllowedReturn";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Matchers.Cast<ArchitectureElement>();

        return result;
    }
}

public sealed class NamespaceHierarchyPolicy(string rootNamespace) : ArchitectureElement, IRootConfigurationElement
{
    public string RootNamespace { get; set; } = rootNamespace;
    public Collection<NamespaceHierarchyBlockedRelation> BlockedRelations { get; } = [];

    internal override string ElementName => "NamespaceHierarchyPolicy";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteRequired("rootNamespace", RootNamespace);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = BlockedRelations.Cast<ArchitectureElement>();

        return result;
    }
}

public sealed class NamespaceHierarchyBlockedRelation(NamespaceHierarchyRelation relation) : SiteFilteredElement
{
    public NamespaceHierarchyRelation Relation { get; set; } = relation;

    internal override string ElementName => "BlockedRelation";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteValue("relation", Relation);
    }
}

public sealed class NameRules : ArchitectureElement, ILayerConfigurationElement
{
    public Collection<ArchitectureElement> Rules { get; } = [];

    internal override string ElementName => "NameRules";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Rules;

        return result;
    }
}

public sealed class RequireMatchingNames : SiteFilteredElement
{
    public NameRuleValueTracking ValueTracking { get; set; } = NameRuleValueTracking.Direct;
    public NameMatcher? Name { get; set; }
    public NameMatcher? Source { get; set; }
    public NameMatcher? Target { get; set; }
    public Collection<NameRuleAllow> AllowedMappings { get; } = [];

    internal override string ElementName => "RequireMatchingNames";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        if (ValueTracking != NameRuleValueTracking.Direct)
        {
            writer.WriteValue("valueTracking", ValueTracking);
        }
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        if (Name is not null)
        {
            yield return CopyName(Name, "Name");
        }

        if (Source is not null)
        {
            yield return CopyName(Source, "Source");
        }

        if (Target is not null)
        {
            yield return CopyName(Target, "Target");
        }

        foreach (var mapping in AllowedMappings)
        {
            yield return mapping;
        }
    }

    private static NameMatcher CopyName(NameMatcher matcher, string name)
    {
        matcher.SetElementName(name);

        return matcher;
    }
}

public sealed class RequireDeclarationNameMatchesType : SiteFilteredElement
{
    public NameMatcher? Type { get; set; }
    public NameMatcher? Name { get; set; }
    public Collection<DeclarationNameRuleAllow> AllowedMappings { get; } = [];

    internal override string ElementName => "RequireDeclarationNameMatchesType";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        if (Type is not null)
        {
            Type.SetElementName("Type");
            yield return Type;
        }

        if (Name is not null)
        {
            Name.SetElementName("Name");
            yield return Name;
        }

        foreach (var mapping in AllowedMappings)
        {
            yield return mapping;
        }
    }
}

public sealed class NameRuleAllow : SiteFilteredElement
{
    public string? From { get; set; }
    public string? To { get; set; }
    public NameMatcher? Source { get; set; }
    public NameMatcher? Target { get; set; }

    internal override string ElementName => "Allow";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteOptional("from", From);
        writer.WriteOptional("to", To);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        if (Source is not null)
        {
            Source.SetElementName("Source");
            yield return Source;
        }

        if (Target is not null)
        {
            Target.SetElementName("Target");
            yield return Target;
        }
    }
}

public sealed class DeclarationNameRuleAllow : SiteFilteredElement
{
    public string? From { get; set; }
    public string? To { get; set; }
    public NameMatcher? Type { get; set; }
    public NameMatcher? Name { get; set; }

    internal override string ElementName => "Allow";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteOptional("from", From);
        writer.WriteOptional("to", To);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        if (Type is not null)
        {
            Type.SetElementName("Type");
            yield return Type;
        }

        if (Name is not null)
        {
            Name.SetElementName("Name");
            yield return Name;
        }
    }
}
