namespace RonSijm.IJzer.IaC;

public sealed class ProjectArchitecture : ArchitectureElement, IRootConfigurationElement
{
    public bool RequireRecognizedProjects { get; set; }
    public Collection<ArchitectureElement> Elements { get; } = [];

    internal override string ElementName => "ProjectArchitecture";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteWhenTrue("requireRecognizedProjects", RequireRecognizedProjects);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Elements;

        return result;
    }
}

public sealed class ProjectGroup(string name) : ArchitectureElement
{
    public string Name { get; set; } = name;
    public Collection<ProjectMatcher> Projects { get; } = [];

    internal override string ElementName => "ProjectGroup";

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

public sealed class ProjectReferenceRule(string from, string to, bool allowed = true) : ArchitectureElement
{
    public string From { get; set; } = from;
    public string To { get; set; } = to;
    public bool Allowed { get; set; } = allowed;
    public Collection<ProjectMatcher> FromMatchers { get; } = [];
    public Collection<ProjectMatcher> ToMatchers { get; } = [];

    internal override string ElementName => Allowed ? "AllowedProjectReference" : "BlockedProjectReference";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteRequired("from", From);
        writer.WriteRequired("to", To);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        foreach (var matcher in FromMatchers)
        {
            yield return CopyProjectMatcher(matcher, "From");
        }

        foreach (var matcher in ToMatchers)
        {
            yield return CopyProjectMatcher(matcher, "To");
        }
    }

    private static ProjectMatcher CopyProjectMatcher(ProjectMatcher matcher, string elementName)
    {
        var result = new ProjectMatcher(elementName)
        {
            TypeName = matcher.TypeName,
            ExactName = matcher.ExactName,
            StartsWith = matcher.StartsWith,
            EndsWith = matcher.EndsWith,
            Contains = matcher.Contains,
            Regex = matcher.Regex,
            Description = matcher.Description
        };

        return result;
    }
}

public sealed class PackagePolicy(string projectGroup) : ArchitectureElement
{
    public string ProjectGroup { get; set; } = projectGroup;
    public bool IncludeTransitive { get; set; }
    public Collection<PackageRuleSet> RuleSets { get; } = [];

    internal override string ElementName => "PackagePolicy";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteRequired("projectGroup", ProjectGroup);
        writer.WriteWhenTrue("includeTransitive", IncludeTransitive);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = RuleSets.Cast<ArchitectureElement>();

        return result;
    }
}

public sealed class PackageRuleSet(bool allowed) : ArchitectureElement
{
    public bool Allowed { get; set; } = allowed;
    public Collection<PackageMatcher> Packages { get; } = [];

    internal override string ElementName => Allowed ? "Allowed" : "Forbidden";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Packages.Cast<ArchitectureElement>();

        return result;
    }
}

public sealed class AssemblyReferencePolicy(string projectGroup) : ArchitectureElement
{
    public string ProjectGroup { get; set; } = projectGroup;
    public Collection<AssemblyReferenceRuleSet> RuleSets { get; } = [];

    internal override string ElementName => "AssemblyReferencePolicy";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteRequired("projectGroup", ProjectGroup);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = RuleSets.Cast<ArchitectureElement>();

        return result;
    }
}

public sealed class AssemblyReferenceRuleSet(bool allowed) : ArchitectureElement
{
    public bool Allowed { get; set; } = allowed;
    public Collection<AssemblyReferenceMatcher> Assemblies { get; } = [];

    internal override string ElementName => Allowed ? "Allowed" : "Forbidden";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Assemblies.Cast<ArchitectureElement>();

        return result;
    }
}
