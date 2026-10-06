using System.Globalization;

namespace RonSijm.AnaalIJzer.IaC;

public sealed class Exceptions : ArchitectureElement
{
    public Collection<ExceptionMatcher> Matchers { get; } = [];

    internal override string ElementName => "Exceptions";

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        var result = Matchers.Cast<ArchitectureElement>();

        return result;
    }
}

public abstract class ExceptionMatcher : ArchitectureElement
{
    public string? Reason { get; set; }
    public string? Owner { get; set; }
    public DateTime? ExpiresOn { get; set; }

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        writer.WriteOptional("reason", Reason);
        writer.WriteOptional("owner", Owner);
        writer.WriteOptional("expiresOn", ExpiresOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
}

public sealed class ClassException : ExceptionMatcher
{
    public ClassMatcher Matcher { get; } = new();
    public Exceptions? Exceptions { get; set; }

    internal override string ElementName => "Class";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        Matcher.WriteAttributes(writer);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        foreach (var declaration in Matcher.Declarations)
        {
            yield return declaration;
        }

        if (Exceptions is not null)
        {
            yield return Exceptions;
        }
    }
}

public sealed class NamespaceException : ExceptionMatcher
{
    public NamespaceMatcher Matcher { get; } = new();
    public Exceptions? Exceptions { get; set; }

    internal override string ElementName => "Namespace";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        Matcher.WriteAttributes(writer);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        if (Exceptions is not null)
        {
            yield return Exceptions;
        }
    }
}

public sealed class AssemblyException : ExceptionMatcher
{
    public AssemblyMatcher Matcher { get; } = new();
    public Exceptions? Exceptions { get; set; }

    internal override string ElementName => "Assembly";

    internal override void WriteAttributes(ArchitectureElementWriter writer)
    {
        base.WriteAttributes(writer);
        Matcher.WriteAttributes(writer);
    }

    internal override IEnumerable<ArchitectureElement> GetChildren()
    {
        if (Exceptions is not null)
        {
            yield return Exceptions;
        }
    }
}
