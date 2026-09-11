using CarRental.Application.Dtos;

namespace CarRental.Application.Queries.CheckCarAvailability;

public interface ICheckCarAvailabilityQueryHandler
{
    Task<IReadOnlyCollection<AvailableCarDto>> HandleAsync(
        CheckCarAvailabilityQuery query,
        CancellationToken cancellationToken = default);
}
