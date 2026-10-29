namespace RonSijm.IJzer.IaC;

public sealed class OperationContainingTypeMatcher : TypedMatcherElement
{
    internal override string ElementName => "ContainingType";
}

public sealed class OperationMemberMatcher : TypedMatcherElement
{
    public Collection<SemanticOperationMemberKind> MemberKinds { get; } = [];

    internal override string ElementName => "Member";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteList("memberKind", MemberKinds);
    }
}

public sealed class OperationMatcher : ArchitectureElement
{
    public Collection<SemanticOperationKind> Kinds { get; } = [];
    public bool? StaticAccess { get; set; }
    public OperationContainingTypeMatcher? ContainingType { get; set; }
    public OperationMemberMatcher? Member { get; set; }

    internal override string ElementName => "OperationMatcher";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteList("kind", Kinds);
        writer.WriteOptional("staticAccess", StaticAccess);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        if (ContainingType is not null)
        {
            yield return ContainingType;
        }

        if (Member is not null)
        {
            yield return Member;
        }
    }
}

public sealed class OperationDeclarationMatcher : ArchitectureElement
{
    public OperationContainingTypeMatcher? ContainingType { get; set; }
    public OperationMemberMatcher? Member { get; set; }

    internal override string ElementName => "DeclarationMatcher";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        if (ContainingType is not null)
        {
            yield return ContainingType;
        }

        if (Member is not null)
        {
            yield return Member;
        }
    }
}

public sealed class RelatedOperations(string elementName) : ArchitectureElement
{
    public Collection<OperationMatcher> Matchers { get; } = [];

    internal override string ElementName { get; } = elementName;

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Matchers.Cast<ArchitectureElement>();

        return result;
    }
}
