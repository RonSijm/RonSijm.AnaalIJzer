namespace RonSijm.AnaalIJzer.Annotations;

/// <summary>Marks a CLR-visible declaration as usable only from its declaring assembly.</summary>
[AttributeUsage(ArchitectureInternalAttributeTargets.All, AllowMultiple = false, Inherited = false)]
public sealed class AssemblyInternalAttribute : Attribute;
