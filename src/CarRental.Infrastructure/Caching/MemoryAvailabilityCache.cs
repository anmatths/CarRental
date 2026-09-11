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
    private long _generation;

    public long Generation => Volatile.Read(ref _generation);

    public void Invalidate()
    {
        var generation = Interlocked.Increment(ref _generation);
        logger.LogDebug("Availability cache invalidated; generation advanced to {AvailabilityCacheGeneration}", generation);
    }

    public async Task<IReadOnlyCollection<AvailableCarDto>> GetOrCreateAsync(
        CheckCarAvailabilityQuery query,
        Func<CancellationToken, Task<IReadOnlyCollection<AvailableCarDto>>> factory,
        CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var generation = Volatile.Read(ref _generation);
            var key = CreateKey(query, generation);
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

            if (generation == Volatile.Read(ref _generation))
                return cachedResult;

            cache.Remove(key);
        }
    }

    public static string CreateKey(CheckCarAvailabilityQuery query, long generation = 0)
    {
        var normalizedQuery = query.NormalizeFilters();

        return FormattableString.Invariant(
            $"availability:v{generation}:{normalizedQuery.StartDate:yyyy-MM-dd}:{normalizedQuery.EndDate:yyyy-MM-dd}:{EscapeFilter(normalizedQuery.Type)}:{EscapeFilter(normalizedQuery.Model)}");
    }

    private static string EscapeFilter(string? value) => value is null ? "-" : Uri.EscapeDataString(value);
}
