namespace RonSijm.IJzer.IaC;

public sealed class Include(string path) : ArchitectureElement, IRootConfigurationElement
{
    public string Path { get; set; } = path;
    public bool AllowNoMatches { get; set; }

    internal override string ElementName => "Include";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteRequired("path", Path);
        writer.WriteWhenTrue("allowNoMatches", AllowNoMatches);
    }
}

public sealed class ExceptionPolicy : ArchitectureElement, IRootConfigurationElement
{
    public bool RequireReason { get; set; }
    public bool RequireOwner { get; set; }
    public bool RequireExpiresOn { get; set; }
    public int? WarnBeforeDays { get; set; }

    internal override string ElementName => "ExceptionPolicy";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteWhenTrue("requireReason", RequireReason);
        writer.WriteWhenTrue("requireOwner", RequireOwner);
        writer.WriteWhenTrue("requireExpiresOn", RequireExpiresOn);
        writer.WriteOptional("warnBeforeDays", WarnBeforeDays);
    }
}

public sealed class Layer(string name) : ArchitectureElement, IRootConfigurationElement, ILayerConfigurationElement
{
    public string Name { get; set; } = name;
    public Collection<DependencySite> RequireRecognizedDependencies { get; } = [];
    public Collection<ILayerConfigurationElement> Elements { get; } = [];

    internal override string ElementName => "Layer";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteRequired("name", Name);
        writer.WriteList("requireRecognizedDependencies", RequireRecognizedDependencies);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        foreach (var item in Elements)
        {
            if (item is not ArchitectureElement element)
            {
                throw new InvalidOperationException($"Layer element '{item.GetType().FullName}' does not derive from {nameof(ArchitectureElement)}.");
            }

            yield return element;
        }
    }
}

public sealed class AllowedTypes : ArchitectureElement, IRootConfigurationElement, ILayerConfigurationElement
{
    public Collection<ArchitectureElement> Matchers { get; } = [];

    internal override string ElementName => "Allowed";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Matchers;

        return result;
    }
}

public sealed class ForbiddenTypes : ArchitectureElement, IRootConfigurationElement, ILayerConfigurationElement
{
    public Collection<ArchitectureElement> Matchers { get; } = [];

    internal override string ElementName => "Forbidden";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Matchers;

        return result;
    }
}

public sealed class ForbiddenClassMatcher : TypedMatcherElement
{
    public RenameFix? Fix { get; set; }
    public Collection<DeclarationMatcher> Declarations { get; } = [];
    public Exceptions? Exceptions { get; set; }

    internal override string ElementName => "Class";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        if (Fix is not null)
        {
            yield return Fix;
        }

        foreach (var declaration in Declarations)
        {
            yield return declaration;
        }

        if (Exceptions is not null)
        {
            yield return Exceptions;
        }
    }
}

public sealed class ForbiddenNamespaceMatcher : StandardMatcherElement
{
    public Exceptions? Exceptions { get; set; }

    internal override string ElementName => "Namespace";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        if (Exceptions is not null)
        {
            yield return Exceptions;
        }
    }
}

public sealed class RenameFix(string rename) : ArchitectureElement
{
    public string Rename { get; set; } = rename;

    internal override string ElementName => "Fix";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteRequired("Rename", Rename);
    }
}

public abstract class DependencyRule(string from, string to) : SiteFilteredElement, IRootConfigurationElement, ILayerConfigurationElement
{
    public string From { get; set; } = from;
    public string To { get; set; } = to;
    public bool AppliesToDescendants { get; set; }

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteRequired("from", From);
        writer.WriteRequired("to", To);
        writer.WriteWhenTrue("appliesToDescendants", AppliesToDescendants);
    }
}

public sealed class AllowedDependency(string from, string to) : DependencyRule(from, to)
{
    internal override string ElementName => "AllowedDependency";
}

public sealed class BlockedDependency(string from, string to) : DependencyRule(from, to)
{
    internal override string ElementName => "BlockedDependency";
}
