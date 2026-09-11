using System.ComponentModel.DataAnnotations;

namespace CarRental.Api.Contracts.Rentals;

public sealed class ModifyRentalRequest : IValidatableObject
{
    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndDate <= StartDate)
        {
            yield return new ValidationResult(
                "The rental end date must be later than the start date.",
                [nameof(EndDate)]);
        }
    }
}
