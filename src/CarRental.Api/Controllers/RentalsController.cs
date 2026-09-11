using CarRental.Api.Contracts.Rentals;
using CarRental.Application.Commands.RegisterRental;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Api.Controllers;

[ApiController]
[Route("api/rentals")]
public sealed class RentalsController(RegisterRentalCommandHandler registerRentalHandler) : ControllerBase
{
    /// <summary>Registers a rental for an available car during the half-open period [startDate, endDate).</summary>
    [HttpPost]
    [ProducesResponseType<RentalResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RentalResponse>> Register(
        RegisterRentalRequest request,
        CancellationToken cancellationToken)
    {
        var rental = await registerRentalHandler.HandleAsync(
            new RegisterRentalCommand(request.CustomerId, request.CarId, request.StartDate, request.EndDate),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, RentalResponse.From(rental));
    }
}
