// ReSharper disable All - Justification: Example File
using System;
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Showcase">
    <Class endsWith="Showcase" />
  </Layer>
  <Layer name="Ingredient">
    <Class endsWith="Ingredient" />
    <Class typeName="IngredientAttribute" />
  </Layer>
  <AllowedDependency from="Showcase" to="Ingredient" />
</ArchitecturalLevels>
""")]

namespace Example.VisualStudioSiteDiagnostics;

// Open this file in Visual Studio and enable "Show all site diagnostics" or
// "Show all layer information" under AnaalIJzer > Editor. Each labeled section
// contains one of the supported dependency-site shapes and builds cleanly.
[Ingredient]
public sealed class AllSiteDiagnosticsShowcase : BaseIngredient, IShowcaseIngredient
{
    // Field
    private readonly FieldIngredient fieldIngredient = new();

    // Constructor
    public AllSiteDiagnosticsShowcase(ConstructorIngredient constructorIngredient)
    {
        ConstructorIngredient = constructorIngredient;
    }

    public ConstructorIngredient ConstructorIngredient { get; }

    // Property
    public PropertyIngredient PropertyIngredient { get; } = new();

    // Method
    public void UseMethodIngredient(MethodIngredient methodIngredient)
    {
        _ = methodIngredient;
    }

    // MethodReturn
    public MethodReturnIngredient CreateMethodReturnIngredient()
    {
        return new MethodReturnIngredient();
    }

    public void ExerciseExpressionSites()
    {
        // Local
        LocalIngredient localIngredient = new();
        _ = localIngredient;

        // New
        var newIngredient = new NewIngredient();
        _ = newIngredient;

        // GenericInvocation
        _ = GenericInvocationIngredient.Resolve<GenericInvocationValueIngredient>();

        // GenericArgument
        Lazy<GenericArgumentIngredient> genericArgumentIngredient = null!;
        _ = genericArgumentIngredient;

        // StaticMember
        StaticMemberIngredient.Use();
    }
}

// Inheritance
public class BaseIngredient;

// InterfaceImplementation
public interface IShowcaseIngredient;

public sealed class ConstructorIngredient;

public sealed class MethodIngredient;

public sealed class MethodReturnIngredient;

public sealed class FieldIngredient;

public sealed class PropertyIngredient;

public sealed class LocalIngredient;

public sealed class NewIngredient;

public static class GenericInvocationIngredient
{
    public static T Resolve<T>() where T : class
    {
        T result = null!;

        return result;
    }
}

public sealed class GenericInvocationValueIngredient;

public sealed class GenericArgumentIngredient;

// Attribute
public sealed class IngredientAttribute : Attribute;

public sealed class StaticMemberIngredient
{
    public static void Use() { }
}
