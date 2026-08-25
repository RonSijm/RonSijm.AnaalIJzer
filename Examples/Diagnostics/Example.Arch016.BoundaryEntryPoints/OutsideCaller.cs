// ReSharper disable All - Justification: Example File
using SweetShop.Ordering.Contracts;
using SweetShop.Ordering.Implementation;

// Valid: Presentation enters Ordering through Contracts.
public class CandyController(PlaceCandyContract contract)
{
}

// ARCH016: Presentation should not enter Ordering through Implementation.
public class CandyAdminController(CandyOrderingService service)
{
}
