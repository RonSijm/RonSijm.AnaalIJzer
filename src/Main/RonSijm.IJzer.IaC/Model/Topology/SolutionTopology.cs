namespace RonSijm.IJzer.IaC;

public sealed class SolutionTopology : ArchitectureElement, IRootConfigurationElement
{
    public bool RequireRecognizedProjects { get; set; }
    public bool EnforceAcyclic { get; set; }
    public Collection<ArchitectureElement> Elements { get; } = [];

    internal override string ElementName => "SolutionTopology";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteWhenTrue("requireRecognizedProjects", RequireRecognizedProjects);
        writer.WriteWhenTrue("enforceAcyclic", EnforceAcyclic);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Elements;

        return result;
    }
}

public sealed class SolutionModule(string name) : ArchitectureElement
{
    public string Name { get; set; } = name;
    public Collection<ProjectMatcher> Projects { get; } = [];

    internal override string ElementName => "Module";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteRequired("name", Name);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Projects.Cast<ArchitectureElement>();

        return result;
    }
}

public sealed class SolutionModuleReference(string from, string to, bool allowed = true) : ArchitectureElement
{
    public string From { get; set; } = from;
    public string To { get; set; } = to;
    public bool Allowed { get; set; } = allowed;

    internal override string ElementName => Allowed ? "AllowedModuleReference" : "BlockedModuleReference";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteRequired("from", From);
        writer.WriteRequired("to", To);
    }
}
