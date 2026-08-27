namespace RonSijm.AnaalIJzer.Core.Indicators;

public static class DependencySites
{
    public const string Constructor = "Constructor";
    public const string Method = "Method";
    public const string MethodReturn = "MethodReturn";
    public const string Field = "Field";
    public const string Property = "Property";
    public const string Local = "Local";
    public const string New = "New";
    public const string GenericInvocation = "GenericInvocation";
    public const string GenericArgument = "GenericArgument";
    public const string Inheritance = "Inheritance";
    public const string InterfaceImplementation = "InterfaceImplementation";
    public const string Attribute = "Attribute";
    public const string StaticMember = "StaticMember";

    public static readonly string[] All =
    [
        Constructor,
        Method,
        MethodReturn,
        Field,
        Property,
        Local,
        New,
        GenericInvocation,
        GenericArgument,
        Inheritance,
        InterfaceImplementation,
        Attribute,
        StaticMember,
    ];

    public static bool TryNormalize(string value, out string normalized)
    {
        foreach (var site in All)
        {
            if (string.Equals(value, site, StringComparison.OrdinalIgnoreCase))
            {
                normalized = site;
                return true;
            }
        }

        normalized = string.Empty;
        return false;
    }
}
