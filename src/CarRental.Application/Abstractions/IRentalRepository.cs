using CarRental.Domain.Entities;

namespace CarRental.Application.Abstractions;

public interface IRentalRepository
{
    Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Rental>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Rental>> GetActiveRentalsForCarAsync(
        Guid carId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    Task AddAsync(Rental rental, CancellationToken cancellationToken = default);

    Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default);
}
