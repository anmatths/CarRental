using CarRental.Application.Dtos;
using CarRental.Application.Queries.CheckCarAvailability;

namespace CarRental.Application.Abstractions;

public interface IAvailabilityCache
{
    Task<IReadOnlyCollection<AvailableCarDto>> GetOrCreateAsync(
        CheckCarAvailabilityQuery query,
        Func<CancellationToken, Task<IReadOnlyCollection<AvailableCarDto>>> factory,
        CancellationToken cancellationToken = default);
}
