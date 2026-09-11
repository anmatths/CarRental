namespace CarRental.Application.Exceptions;

public sealed class RentalNotFoundException(Guid rentalId)
    : Exception($"Rental '{rentalId}' was not found.");
