namespace RonSijm.AnaalIJzer.IaC;

public sealed class AssemblyAttributePolicy : ArchitectureElement, IRootConfigurationElement
{
    public Collection<AssemblyAttributeRuleSet> RuleSets { get; } = [];

    internal override string ElementName => "AssemblyAttributePolicy";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = RuleSets.Cast<ArchitectureElement>();

        return result;
    }
}

public sealed class AssemblyAttributeRuleSet(bool allowed) : ArchitectureElement
{
    public bool Allowed { get; set; } = allowed;
    public Collection<AssemblyAttributeMatcher> Attributes { get; } = [];

    internal override string ElementName => Allowed ? "Allowed" : "Forbidden";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Attributes.Cast<ArchitectureElement>();

        return result;
    }
}

public sealed class AssemblyAttributeMatcher : TypedMatcherElement
{
    public Collection<AssemblyAttributeArgumentMatcher> Arguments { get; } = [];

    internal override string ElementName => "Attribute";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Arguments.Cast<ArchitectureElement>();

        return result;
    }
}

public sealed class AssemblyAttributeArgumentMatcher : TextMatcherElement
{
    public int? Index { get; set; }
    public string? Name { get; set; }

    internal override string ElementName => "Argument";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteOptional("index", Index);
        writer.WriteOptional("name", Name);
    }
}
