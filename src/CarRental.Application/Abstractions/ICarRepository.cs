using CarRental.Domain.Entities;

namespace CarRental.Application.Abstractions;

public interface ICarRepository
{
    Task<Car?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Car>> GetByCriteriaAsync(
        string? type,
        string? model,
        CancellationToken cancellationToken = default);
}
