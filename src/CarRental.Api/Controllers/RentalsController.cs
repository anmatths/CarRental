using CarRental.Api.Contracts.Rentals;
using CarRental.Application.Commands.CancelRental;
using CarRental.Application.Commands.ModifyRental;
using CarRental.Application.Commands.RegisterRental;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Api.Controllers;

[ApiController]
[Route("api/rentals")]
public sealed class RentalsController(
    RegisterRentalCommandHandler registerRentalHandler,
    ModifyRentalCommandHandler modifyRentalHandler,
    CancelRentalCommandHandler cancelRentalHandler) : ControllerBase
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

    /// <summary>Changes the period of an active rental, using the half-open period [startDate, endDate).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<RentalResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RentalResponse>> Modify(
        Guid id,
        ModifyRentalRequest request,
        CancellationToken cancellationToken)
    {
        var rental = await modifyRentalHandler.HandleAsync(
            new ModifyRentalCommand(id, request.StartDate, request.EndDate),
            cancellationToken);

        return Ok(RentalResponse.From(rental));
    }

    /// <summary>Cancels a rental without deleting its persisted record.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await cancelRentalHandler.HandleAsync(new CancelRentalCommand(id), cancellationToken);
        return NoContent();
    }
}
