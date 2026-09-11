namespace CarRental.Domain.Exceptions;

public sealed class InvalidRentalPeriodException : DomainValidationException
{
    public InvalidRentalPeriodException(string message)
        : base(message)
    {
    }
}
