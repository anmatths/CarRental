using CarRental.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarRental.IntegrationTests;

public sealed class DevelopmentDataSeederTests
{
    [Fact]
    public async Task SeedAsync_OnCleanDatabase_AddsDemoCars()
    {
        await using var dbContext = CreateDbContext();

        await DevelopmentDataSeeder.SeedAsync(dbContext);

        var cars = await dbContext.Cars.OrderBy(car => car.Model).ToListAsync();
        Assert.Equal(DevelopmentDataSeeder.DemoCars.Count, cars.Count);
        Assert.Equal(
            DevelopmentDataSeeder.DemoCars.Select(car => car.Id).Order(),
            cars.Select(car => car.Id).Order());
    }

    [Fact]
    public async Task SeedAsync_WhenRepeated_DoesNotDuplicateDemoCars()
    {
        await using var dbContext = CreateDbContext();

        await DevelopmentDataSeeder.SeedAsync(dbContext);
        await DevelopmentDataSeeder.SeedAsync(dbContext);

        Assert.Equal(DevelopmentDataSeeder.DemoCars.Count, await dbContext.Cars.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_AddsCarsWithDifferentTypesAndModels()
    {
        await using var dbContext = CreateDbContext();

        await DevelopmentDataSeeder.SeedAsync(dbContext);

        var cars = await dbContext.Cars.ToListAsync();
        Assert.True(cars.Select(car => car.Type).Distinct().Count() > 1);
        Assert.True(cars.Select(car => car.Model).Distinct().Count() > 1);
    }

    private static CarRentalDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<CarRentalDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
