using CarRental.Application.Dtos;
using CarRental.Application.Queries.CheckCarAvailability;

namespace CarRental.Application.Abstractions;

public interface IAvailabilityCache
{
    /// <summary>Invalidates all availability results by advancing their cache generation.</summary>
    void Invalidate();

    Task<IReadOnlyCollection<AvailableCarDto>> GetOrCreateAsync(
        CheckCarAvailabilityQuery query,
        Func<CancellationToken, Task<IReadOnlyCollection<AvailableCarDto>>> factory,
        CancellationToken cancellationToken = default);
}
