using CarRental.Application.Abstractions;
using CarRental.Application.Dtos;
using CarRental.Application.Queries.CheckCarAvailability;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace CarRental.Infrastructure.Caching;

public sealed class MemoryAvailabilityCache(
    IMemoryCache cache,
    ILogger<MemoryAvailabilityCache> logger) : IAvailabilityCache
{
    private static readonly TimeSpan AbsoluteExpiration = TimeSpan.FromMinutes(3);

    public async Task<IReadOnlyCollection<AvailableCarDto>> GetOrCreateAsync(
        CheckCarAvailabilityQuery query,
        Func<CancellationToken, Task<IReadOnlyCollection<AvailableCarDto>>> factory,
        CancellationToken cancellationToken = default)
    {
        var key = CreateKey(query);
        if (cache.TryGetValue<IReadOnlyCollection<AvailableCarDto>>(key, out var cachedCars))
        {
            logger.LogDebug("Availability cache hit for {AvailabilityCacheKey}", key);
            return cachedCars!;
        }

        logger.LogDebug("Availability cache miss for {AvailabilityCacheKey}", key);
        var cars = await factory(cancellationToken);
        var cachedResult = cars.ToArray();
        cache.Set(key, cachedResult, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = AbsoluteExpiration
        });

        return cachedResult;
    }

    public static string CreateKey(CheckCarAvailabilityQuery query)
    {
        var normalizedQuery = query.NormalizeFilters();

        return FormattableString.Invariant(
            $"availability:{normalizedQuery.StartDate:yyyy-MM-dd}:{normalizedQuery.EndDate:yyyy-MM-dd}:{EscapeFilter(normalizedQuery.Type)}:{EscapeFilter(normalizedQuery.Model)}");
    }

    private static string EscapeFilter(string? value) => value is null ? "-" : Uri.EscapeDataString(value);
}
