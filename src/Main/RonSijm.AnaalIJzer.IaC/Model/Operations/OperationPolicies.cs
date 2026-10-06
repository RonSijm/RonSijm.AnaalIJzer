namespace RonSijm.AnaalIJzer.IaC;

public sealed class ForbiddenOperations : ArchitectureElement, ILayerConfigurationElement
{
    public Collection<ForbiddenOperation> Rules { get; } = [];

    internal override string ElementName => "ForbiddenOperations";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Rules.Cast<ArchitectureElement>();

        return result;
    }
}

public sealed class ForbiddenOperation : SiteFilteredElement
{
    public Collection<OperationMatcher> Matchers { get; } = [];

    internal override string ElementName => "ForbiddenOperation";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Matchers.Cast<ArchitectureElement>();

        return result;
    }
}

public sealed class BehavioralOperations : ArchitectureElement, ILayerConfigurationElement
{
    public Collection<BehavioralOperationRule> Rules { get; } = [];

    internal override string ElementName => "BehavioralOperations";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Rules.Cast<ArchitectureElement>();

        return result;
    }
}

public abstract class BehavioralOperationRule : SiteFilteredElement
{
    public BehavioralOperationOrdering Ordering { get; set; } = BehavioralOperationOrdering.Dominance;
    public OperationDeclarationMatcher DeclarationMatcher { get; set; } = new();
    public Collection<OperationMatcher> OperationMatchers { get; } = [];

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        if (Ordering != BehavioralOperationOrdering.Dominance)
        {
            writer.WriteValue("ordering", Ordering);
        }
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        yield return DeclarationMatcher;
        foreach (var matcher in OperationMatchers)
        {
            yield return matcher;
        }

        foreach (var related in GetRelatedOperations())
        {
            yield return related;
        }
    }

    protected virtual IEnumerable<ArchitectureElement> GetRelatedOperations()
    {
        var result = Array.Empty<ArchitectureElement>();

        return result;
    }
}

public sealed class RequiredOperation : BehavioralOperationRule
{
    internal override string ElementName => "RequiredOperation";
}

public sealed class RequiredOperationBefore : BehavioralOperationRule
{
    public RelatedOperations BeforeOperation { get; } = new("BeforeOperation");

    internal override string ElementName => "RequiredOperationBefore";

    protected override IEnumerable<ArchitectureElement> GetRelatedOperations()
    {
        yield return BeforeOperation;
    }
}

public sealed class ForbiddenOperationAfter : BehavioralOperationRule
{
    public RelatedOperations AfterOperation { get; } = new("AfterOperation");

    internal override string ElementName => "ForbiddenOperationAfter";

    protected override IEnumerable<ArchitectureElement> GetRelatedOperations()
    {
        yield return AfterOperation;
    }
}

public sealed class MaximumOperationCount(int maximum) : BehavioralOperationRule
{
    public int Maximum { get; set; } = maximum;

    internal override string ElementName => "MaximumOperationCount";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteValue("maximum", Maximum);
    }
}
