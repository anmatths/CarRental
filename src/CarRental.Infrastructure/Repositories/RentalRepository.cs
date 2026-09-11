using CarRental.Application.Abstractions;
using CarRental.Application.Exceptions;
using CarRental.Domain.Entities;
using CarRental.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CarRental.Infrastructure.Repositories;

public sealed class RentalRepository(CarRentalDbContext dbContext) : IRentalRepository
{
    internal const string ActiveRentalPeriodConstraint = "EX_Rentals_ActiveCarDateRange";

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
        try
        {
            await dbContext.Rentals.AddAsync(rental, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsActiveRentalPeriodConstraintViolation(exception))
        {
            throw new CarNotAvailableException(rental.CarId);
        }
    }

    public async Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default)
    {
        try
        {
            dbContext.Rentals.Update(rental);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsActiveRentalPeriodConstraintViolation(exception))
        {
            throw new CarNotAvailableException(rental.CarId);
        }
    }

    private static bool IsActiveRentalPeriodConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.ExclusionViolation,
            ConstraintName: ActiveRentalPeriodConstraint
        };
}
