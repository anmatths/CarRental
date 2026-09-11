namespace CarRental.Application.Queries.CheckCarAvailability;

public sealed record CheckCarAvailabilityQuery(DateOnly StartDate, DateOnly EndDate, string? Type = null, string? Model = null)
{
    public CheckCarAvailabilityQuery NormalizeFilters() => this with
    {
        Type = NormalizeOptionalFilter(Type),
        Model = NormalizeOptionalFilter(Model)
    };

    private static string? NormalizeOptionalFilter(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
