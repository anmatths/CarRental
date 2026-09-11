namespace CarRental.Application.Exceptions;

public sealed class CarNotAvailableException(Guid carId)
    : Exception($"Car '{carId}' is not available for the requested period.");
