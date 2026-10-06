namespace RonSijm.AnaalIJzer.IaC;

public sealed class OperationContracts : ArchitectureElement, IRootConfigurationElement
{
    public Collection<OperationContract> Operations { get; } = [];

    internal override string ElementName => "Operations";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Operations.Cast<ArchitectureElement>();

        return result;
    }
}

public sealed class OperationContract(string name, OperationDeclarationSelector owner) : ArchitectureElement
{
    public string Name { get; set; } = name;
    public Collection<string> AllowedOwnerLayers { get; } = [];
    public Collection<string> AllowedEntryPointLayers { get; } = [];
    public OperationDeclarationSelector Owner { get; set; } = owner;
    public OperationTypeSelector? Request { get; set; }
    public OperationTypeSelector? Response { get; set; }
    public Collection<OperationDeclarationSelector> EntryPoints { get; } = [];

    internal override string ElementName => "Operation";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteRequired("name", Name);
        writer.WriteList("allowedOwnerLayers", AllowedOwnerLayers);
        writer.WriteList("allowedEntryPointLayers", AllowedEntryPointLayers);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        Owner.SetElementName("Owner");
        yield return Owner;

        if (Request is not null)
        {
            Request.SetElementName("Request");
            yield return Request;
        }

        if (Response is not null)
        {
            Response.SetElementName("Response");
            yield return Response;
        }

        foreach (var entryPoint in EntryPoints)
        {
            entryPoint.SetElementName("EntryPoint");
            yield return entryPoint;
        }
    }
}

public sealed class OperationDeclarationSelector(OperationDeclarationMatcher declarationMatcher) : ArchitectureElement
{
    private string _elementName = "Owner";

    public OperationDeclarationMatcher DeclarationMatcher { get; set; } = declarationMatcher;

    internal override string ElementName => _elementName;

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        yield return DeclarationMatcher;
    }

    internal void SetElementName(string elementName)
    {
        _elementName = elementName;
    }
}

public sealed class OperationTypeSelector(ClassMatcher matcher) : ArchitectureElement
{
    private string _elementName = "Request";

    public ClassMatcher Matcher { get; set; } = matcher;

    internal override string ElementName => _elementName;

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        yield return Matcher;
    }

    internal void SetElementName(string elementName)
    {
        _elementName = elementName;
    }
}
