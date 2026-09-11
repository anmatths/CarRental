namespace CarRental.Application.Exceptions;

public sealed class CustomerNotFoundException(Guid customerId)
    : Exception($"Customer '{customerId}' was not found.");
