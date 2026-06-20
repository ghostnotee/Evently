using Evently.Common.Domain;

namespace Evently.Modules.Ticketing.Domain.Customers;

public static class CustomerErrors
{
    public static Error NotFound(Guid customerId)
    {
        return Error.NotFound("Customers.NotFound", $"The customer with the identifier {customerId} was not found");
    }
}
