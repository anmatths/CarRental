namespace CarRental.Application.Commands.ModifyRental;

public sealed record ModifyRentalCommand(Guid RentalId, DateOnly StartDate, DateOnly EndDate);
