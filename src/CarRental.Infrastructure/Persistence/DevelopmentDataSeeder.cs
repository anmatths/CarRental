using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence;

/// <summary>
/// Provides the small, deterministic car catalog used to evaluate the API locally.
/// This is invoked only by the API's Development startup path.
/// </summary>
public static class DevelopmentDataSeeder
{
    public static readonly IReadOnlyList<DemoCar> DemoCars =
    [
        new(new Guid("10000000-0000-0000-0000-000000000001"), "Sedan", "Toyota Corolla"),
        new(new Guid("10000000-0000-0000-0000-000000000002"), "Hatchback", "Toyota Yaris"),
        new(new Guid("10000000-0000-0000-0000-000000000003"), "Sedan", "Honda Civic"),
        new(new Guid("10000000-0000-0000-0000-000000000004"), "SUV", "Honda CR-V"),
        new(new Guid("10000000-0000-0000-0000-000000000005"), "Hatchback", "Ford Focus"),
        new(new Guid("10000000-0000-0000-0000-000000000006"), "Pickup", "Ford Ranger"),
        new(new Guid("10000000-0000-0000-0000-000000000007"), "Hatchback", "Volkswagen Golf"),
        new(new Guid("10000000-0000-0000-0000-000000000008"), "SUV", "Volkswagen Taos")
    ];

    public static async Task SeedAsync(CarRentalDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var demoCarIds = DemoCars.Select(car => car.Id).ToArray();
        var existingIds = await dbContext.Cars
            .Where(car => demoCarIds.Contains(car.Id))
            .Select(car => car.Id)
            .ToListAsync(cancellationToken);

        var carsToAdd = DemoCars
            .Where(car => !existingIds.Contains(car.Id))
            .Select(car => new Car(car.Id, car.Type, car.Model));

        dbContext.Cars.AddRange(carsToAdd);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed record DemoCar(Guid Id, string Type, string Model);
