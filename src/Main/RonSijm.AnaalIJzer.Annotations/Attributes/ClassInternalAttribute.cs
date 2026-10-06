namespace RonSijm.AnaalIJzer.Annotations;

/// <summary>
///     Marks a CLR-visible declaration as usable only from its declaring type and that type's
///     nested type family.
/// </summary>
[AttributeUsage(ArchitectureInternalAttributeTargets.All, AllowMultiple = false, Inherited = false)]
public sealed class ClassInternalAttribute : Attribute;
