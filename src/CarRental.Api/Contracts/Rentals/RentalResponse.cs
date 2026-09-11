using CarRental.Application.Dtos;
using CarRental.Domain.Entities;

namespace CarRental.Api.Contracts.Rentals;

public sealed record RentalResponse(
    Guid Id,
    Guid CustomerId,
    Guid CarId,
    DateOnly StartDate,
    DateOnly EndDate,
    RentalStatus Status)
{
    public static RentalResponse From(RentalDto rental) =>
        new(rental.Id, rental.CustomerId, rental.CarId, rental.StartDate, rental.EndDate, rental.Status);
}
