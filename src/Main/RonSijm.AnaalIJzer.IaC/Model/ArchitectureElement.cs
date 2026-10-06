namespace RonSijm.AnaalIJzer.IaC;

public interface IRootConfigurationElement;

public interface ILayerConfigurationElement;

public abstract class ArchitectureElement
{
    public string? Description { get; set; }

    internal abstract string ElementName { get; }

    internal virtual void WriteAttributes(ArchitectureElementWriter writer)
    {
        writer.WriteOptional("description", Description);
    }

    internal virtual IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Array.Empty<ArchitectureElement>();

        return result;
    }
}

public abstract class SiteFilteredElement : ArchitectureElement
{
    public Collection<DependencySite> AllowedSites { get; } = [];

    public Collection<DependencySite> BlockedSites { get; } = [];

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        if (AllowedSites.Count > 0 && BlockedSites.Count > 0)
        {
            throw new InvalidOperationException($"{GetType().Name} cannot define both allowed sites and blocked sites.");
        }

        base.WriteAttributes(writer);
        writer.WriteList("allowedSites", AllowedSites);
        writer.WriteList("blockedSites", BlockedSites);
    }
}
