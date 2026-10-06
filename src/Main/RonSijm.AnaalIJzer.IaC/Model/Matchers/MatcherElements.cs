namespace RonSijm.AnaalIJzer.IaC;

public abstract class StandardMatcherElement : ArchitectureElement
{
    public string? EndsWith { get; set; }
    public string? StartsWith { get; set; }
    public string? Contains { get; set; }
    public string? Regex { get; set; }
    public string? ExactName { get; set; }
    public string? ExactFullName { get; set; }
    public string? Inherits { get; set; }
    public string? Implements { get; set; }
    public string? WithAttribute { get; set; }
    public string? WithAccessModifier { get; set; }
    public string? Comment { get; set; }

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteOptional("endsWith", EndsWith);
        writer.WriteOptional("startsWith", StartsWith);
        writer.WriteOptional("contains", Contains);
        writer.WriteOptional("regex", Regex);
        writer.WriteOptional("exactName", ExactName);
        writer.WriteOptional("exactFullName", ExactFullName);
        writer.WriteOptional("inherits", Inherits);
        writer.WriteOptional("implements", Implements);
        writer.WriteOptional("withAttribute", WithAttribute);
        writer.WriteOptional("withAccessModifier", WithAccessModifier);
        writer.WriteOptional("comment", Comment);
    }
}

public abstract class TypedMatcherElement : StandardMatcherElement
{
    public string? TypeName { get; set; }
    public DeclaredTypeKind? TypeKind { get; set; }

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteOptional("typeName", TypeName);
        writer.WriteOptional("typeKind", TypeKind);
    }
}

public abstract class TextMatcherElement : ArchitectureElement
{
    public string? TypeName { get; set; }
    public string? ExactName { get; set; }
    public string? StartsWith { get; set; }
    public string? EndsWith { get; set; }
    public string? Contains { get; set; }
    public string? Regex { get; set; }

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteOptional("typeName", TypeName);
        writer.WriteOptional("exactName", ExactName);
        writer.WriteOptional("startsWith", StartsWith);
        writer.WriteOptional("endsWith", EndsWith);
        writer.WriteOptional("contains", Contains);
        writer.WriteOptional("regex", Regex);
    }
}

public abstract class TypeMatcherElement : ArchitectureElement;

public sealed class ClassMatcher : TypedMatcherElement, ILayerConfigurationElement
{
    public Collection<DeclarationMatcher> Declarations { get; } = [];
    public Exceptions? Exceptions { get; set; }

    internal override string ElementName => "Class";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
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

public sealed class NamespaceMatcher : StandardMatcherElement, ILayerConfigurationElement
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

public sealed class AssemblyMatcher : ArchitectureElement, ILayerConfigurationElement
{
    public string? ExactName { get; set; }
    public string? EndsWith { get; set; }
    public string? StartsWith { get; set; }
    public string? Contains { get; set; }
    public string? Regex { get; set; }
    public string? Comment { get; set; }
    public Exceptions? Exceptions { get; set; }

    internal override string ElementName => "Assembly";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteOptional("exactName", ExactName);
        writer.WriteOptional("endsWith", EndsWith);
        writer.WriteOptional("startsWith", StartsWith);
        writer.WriteOptional("contains", Contains);
        writer.WriteOptional("regex", Regex);
        writer.WriteOptional("comment", Comment);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        if (Exceptions is not null)
        {
            yield return Exceptions;
        }
    }
}

public sealed class DeclarationMatcher(DeclarationTarget target) : TypedMatcherElement
{
    public DeclarationTarget Target { get; } = target;
    public Collection<CodeObservationMatcher> Observations { get; } = [];

    internal override string ElementName => Target.ToString();

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Observations.Cast<ArchitectureElement>();

        return result;
    }
}

public sealed class CodeObservationMatcher(CodeObservationKind kind) : StandardMatcherElement
{
    public CodeObservationKind Kind { get; } = kind;
    public string? TypeName { get; set; }

    internal override string ElementName => Kind.ToString();

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteOptional("typeName", TypeName);
    }
}

public sealed class NameMatcher(string elementName = "Name") : TypedMatcherElement
{
    private string _elementName = elementName;

    internal override string ElementName => _elementName;

    internal void SetElementName(string elementName)
    {
        _elementName = elementName;
    }
}

public sealed class ReturnValueMatcher(ReturnValueKind kind) : StandardMatcherElement
{
    public ReturnValueKind Kind { get; } = kind;
    public string? Value { get; set; }

    internal override string ElementName => Kind.ToString();

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteOptionalValue("value", Value);
    }
}

public sealed class SourceLocationMatcher : TextMatcherElement
{
    public string? AssemblyName { get; set; }

    internal override string ElementName => "Source";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteOptional("assemblyName", AssemblyName);
    }
}

public sealed class GeneratedCodePathMatcher : TextMatcherElement
{
    internal override string ElementName => "Path";
}

public sealed class ProjectMatcher(string elementName = "Project") : TextMatcherElement
{
    internal override string ElementName { get; } = elementName;
}

public sealed class PackageMatcher : TextMatcherElement
{
    public string? Comment { get; set; }

    internal override string ElementName => "Package";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteOptional("comment", Comment);
    }
}

public sealed class AssemblyReferenceMatcher : TextMatcherElement
{
    public string? Comment { get; set; }

    internal override string ElementName => "Assembly";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteOptional("comment", Comment);
    }
}
