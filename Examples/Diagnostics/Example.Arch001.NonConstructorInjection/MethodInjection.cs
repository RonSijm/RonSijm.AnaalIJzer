// ARCH001: moving the chef dependency to a method does not restore the waiter.
public class MethodDependencyCustomer
{
	public void OrderFrom(IChef chef) { }
}
