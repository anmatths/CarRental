using CarRental.Api.Contracts.Cars;
using CarRental.Application.Dtos;
using CarRental.Application.Queries.CheckCarAvailability;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Api.Controllers;

[ApiController]
[Route("api/cars")]
public sealed class CarsController(ICheckCarAvailabilityQueryHandler availabilityHandler) : ControllerBase
{
    /// <summary>Gets cars available during the requested half-open period [startDate, endDate).</summary>
    [HttpGet("availability")]
    [ProducesResponseType<IReadOnlyCollection<AvailableCarDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyCollection<AvailableCarDto>>> GetAvailability(
        [FromQuery] CheckCarAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        var cars = await availabilityHandler.HandleAsync(
            new CheckCarAvailabilityQuery(
                request.StartDate!.Value,
                request.EndDate!.Value,
                request.Type,
                request.Model),
            cancellationToken);

        return Ok(cars);
    }
}
