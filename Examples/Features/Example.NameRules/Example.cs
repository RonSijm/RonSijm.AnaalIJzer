// ReSharper disable All - Justification: Example File

using System.Reflection;

[assembly: AssemblyMetadata("AnaalIJzerSettings", $"""
<ArchitecturalLevels>
  <Layer name="Application">
    <Class endsWith="Service" />
    <NameRules>
      <RequireMatchingNames>
        <Name endsWith="Id" />
        <Allow from="legacyCustomerId" to="customerId" allowedSites="Constructor" />
      </RequireMatchingNames>
    </NameRules>
  </Layer>
</ArchitecturalLevels>
""")]

namespace Example.NameRules;

public sealed class Customer
{
    public Customer(int customerId)
    {
    }

    public int Id { get; set; }
}

public sealed class OrderService
{
    // Valid: customerId normalizes to Customer.Id.
    public void AssignCustomer(Customer customer, int customerId)
    {
        customer.Id = customerId;
        Save(customerId);
    }

    // Valid: legacyCustomerId is an intentional constructor-only translation.
    public void CreateCustomer(int legacyCustomerId)
    {
        _ = new Customer(legacyCustomerId);
    }

    // ARCH008: the same legacy mapping is not allowed for method arguments.
    public void SaveLegacyId(int legacyCustomerId)
    {
        Save(legacyCustomerId);
    }

    // ARCH008: fruitId and animalId are both Id-like names, but they are swapped.
    public void SwapIds()
    {
        var fruitId = 1;
        var animalId = 2;

        Log(animalId, fruitId);
    }

    // ARCH008: local assignment still has to preserve the name.
    public void StoreLocal(int animalId)
    {
        var fruitId = animalId;
    }

    private static void Save(int customerId)
    {
    }

    private static void Log(int fruitId, int animalId)
    {
    }
}
