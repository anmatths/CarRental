using CarRental.Application.Abstractions;
using CarRental.Domain.Entities;
using CarRental.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Repositories;

public sealed class RentalRepository(CarRentalDbContext dbContext) : IRentalRepository
{
    public Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Rentals.SingleOrDefaultAsync(rental => rental.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Rental>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Rentals.AsNoTracking().ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Rental>> GetActiveRentalsForCarAsync(
        Guid carId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default) =>
        await dbContext.Rentals
            .AsNoTracking()
            .Where(rental =>
                rental.CarId == carId &&
                rental.Status == RentalStatus.Active &&
                rental.StartDate < endDate &&
                rental.EndDate > startDate)
            .ToArrayAsync(cancellationToken);

    public async Task AddAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        await dbContext.Rentals.AddAsync(rental, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        dbContext.Rentals.Update(rental);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
