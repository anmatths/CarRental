namespace CarRental.Application.Commands.RegisterRental;

public sealed record RegisterRentalCommand(Guid CustomerId, Guid CarId, DateOnly StartDate, DateOnly EndDate);
