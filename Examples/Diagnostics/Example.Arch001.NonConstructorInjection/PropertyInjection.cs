// ARCH001: a settable property is still a direct Customer -> Chef dependency.
public class PropertyDependencyCustomer
{
	public IChef Chef { get; set; } = null!;
}
