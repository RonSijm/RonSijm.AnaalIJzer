// ARCH001: creating a chef directly still bypasses the waiter.
public class NewingCustomer
{
	public void Run()
	{
		var chef = new DirectChef();
		_ = chef;
	}
}
