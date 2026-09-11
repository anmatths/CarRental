namespace CarRental.Application.Queries.CheckCarAvailability;

public sealed record CheckCarAvailabilityQuery(DateOnly StartDate, DateOnly EndDate, string? Type = null, string? Model = null);
