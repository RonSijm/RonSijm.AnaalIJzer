namespace RonSijm.AnaalIJzer.Annotations;

/// <summary>
///     Marks a CLR-visible declaration as usable only from its configured architectural layer
///     and descendant layers.
/// </summary>
[AttributeUsage(ArchitectureInternalAttributeTargets.All, AllowMultiple = false, Inherited = false)]
public sealed class LayerInternalAttribute : Attribute;
