using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Api.Contracts.Cars;

public sealed class CheckCarAvailabilityRequest : IValidatableObject
{
    [Required]
    [FromQuery(Name = "startDate")]
    public DateOnly? StartDate { get; init; }

    [Required]
    [FromQuery(Name = "endDate")]
    public DateOnly? EndDate { get; init; }

    [FromQuery(Name = "type")]
    public string? Type { get; init; }

    [FromQuery(Name = "model")]
    public string? Model { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && EndDate.HasValue && EndDate <= StartDate)
        {
            yield return new ValidationResult(
                "EndDate must be later than StartDate.",
                [nameof(EndDate)]);
        }
    }
}
