using CarRental.Application.Abstractions;
using CarRental.Domain.Entities;
using CarRental.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Repositories;

public sealed class CarRepository(CarRentalDbContext dbContext) : ICarRepository
{
    public Task<Car?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Cars.AsNoTracking().SingleOrDefaultAsync(car => car.Id == id, cancellationToken);

    public async Task<IReadOnlyCollection<Car>> GetByCriteriaAsync(string? type, string? model, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Cars.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(car => car.Type == type);
        }

        if (!string.IsNullOrWhiteSpace(model))
        {
            query = query.Where(car => car.Model == model);
        }

        return await query.ToArrayAsync(cancellationToken);
    }
}
