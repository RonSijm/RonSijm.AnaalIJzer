// ReSharper disable All - Justification: Example File

#nullable disable

using System;
using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", """
<ArchitecturalLevels>
  <Layer name="Kitchen">
    <Class endsWith="Kitchen" />
    <ReturnValuePolicy description="Nullable lookup results need a real serving decision.">
      <Invocation
        withAttribute="JetBrains.Annotations.CanBeNullAttribute"
        description="A nullable lookup is handled before it leaves the kitchen." />
    </ReturnValuePolicy>
  </Layer>
</ArchitecturalLevels>
""")]

namespace JetBrains.Annotations
{
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class CanBeNullAttribute : Attribute { }
}

namespace Example.Arch020.AnnotatedInvocationReturn
{

public sealed class Pizza
{
	public static Pizza Margherita { get; } = new Pizza();
}

public sealed class PizzaLookup
{
	[JetBrains.Annotations.CanBeNull]
	public Pizza FindTodaySpecial()
	{
		return null;
	}
}

public sealed class PizzaKitchen(PizzaLookup lookup)
{
	// ARCH020: the lookup may have no pizza, so it cannot leave the kitchen unchanged.
	public Pizza ServeTodaySpecial()
	{
		return lookup.FindTodaySpecial();
	}

	// Valid: the kitchen turns a missing special into a real menu choice.
	public Pizza ServeTodaySpecialOrMargherita()
	{
		return lookup.FindTodaySpecial() ?? Pizza.Margherita;
	}
}
}
