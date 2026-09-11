namespace CarRental.Domain.Exceptions;

public sealed class InvalidRentalOperationException : DomainValidationException
{
    public InvalidRentalOperationException(string message)
        : base(message)
    {
    }
}
