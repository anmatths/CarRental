using CarRental.Application.Abstractions;
using CarRental.Application.Dtos;

namespace CarRental.Application.Queries.CheckCarAvailability;

/// <summary>Decorates availability queries with cache lookup and population.</summary>
public sealed class CachedCheckCarAvailabilityQueryHandler(
    CheckCarAvailabilityQueryHandler innerHandler,
    IAvailabilityCache availabilityCache) : ICheckCarAvailabilityQueryHandler
{
    public Task<IReadOnlyCollection<AvailableCarDto>> HandleAsync(
        CheckCarAvailabilityQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query.NormalizeFilters();

        return availabilityCache.GetOrCreateAsync(
            normalizedQuery,
            innerCancellationToken => innerHandler.HandleAsync(normalizedQuery, innerCancellationToken),
            cancellationToken);
    }
}
