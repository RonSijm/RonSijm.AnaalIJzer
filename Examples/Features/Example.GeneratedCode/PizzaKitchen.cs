// ReSharper disable All - Justification: Example File

using System;

namespace Example.GeneratedCode;

public sealed class PizzaKitchen
{
	// Ordinary source is always analyzed; this method stays valid because its time comes from an explicit input.
	public DateTime Prepare(DateTime servingTime)
	{
		return servingTime;
	}
}
