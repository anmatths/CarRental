using CarRental.Domain.Entities;

namespace CarRental.Application.Dtos;

public sealed record RentalDto(
    Guid Id,
    Guid CustomerId,
    Guid CarId,
    DateOnly StartDate,
    DateOnly EndDate,
    RentalStatus Status);
