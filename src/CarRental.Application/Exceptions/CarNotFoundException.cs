namespace CarRental.Application.Exceptions;

public sealed class CarNotFoundException(Guid carId)
    : Exception($"Car '{carId}' was not found.");
