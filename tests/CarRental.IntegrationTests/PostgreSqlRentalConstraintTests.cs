using CarRental.Application.Exceptions;
using CarRental.Domain.Entities;
using CarRental.Infrastructure.Persistence;
using CarRental.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace CarRental.IntegrationTests;

public sealed class PostgreSqlRentalConstraintTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime
{
    private const string ActiveRentalPeriodConstraint = "EX_Rentals_ActiveCarDateRange";
    private readonly PostgreSqlFixture _fixture;

    public PostgreSqlRentalConstraintTests(PostgreSqlFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task OverlappingActiveRentalsForTheSameCar_AreRejectedByPostgreSql()
    {
        var (customer, car) = await SeedCustomerAndCarAsync();
        await AddRentalAsync(Rental.Create(customer.Id, car.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5)));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() =>
            AddRentalAsync(Rental.Create(customer.Id, car.Id, new DateOnly(2026, 10, 4), new DateOnly(2026, 10, 10))));
        var postgresException = Assert.IsType<PostgresException>(exception.InnerException);

        Assert.Equal(PostgresErrorCodes.ExclusionViolation, postgresException.SqlState);
        Assert.Equal(ActiveRentalPeriodConstraint, postgresException.ConstraintName);
    }

    [Fact]
    public async Task ConsecutiveActiveRentalsForTheSameCar_AreAllowed()
    {
        var (customer, car) = await SeedCustomerAndCarAsync();
        await AddRentalAsync(Rental.Create(customer.Id, car.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5)));

        await AddRentalAsync(Rental.Create(customer.Id, car.Id, new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 10)));
    }

    [Fact]
    public async Task OverlappingActiveRentalsForDifferentCars_AreAllowed()
    {
        var (customer, firstCar, secondCar) = await SeedCustomerAndTwoCarsAsync();
        await AddRentalAsync(Rental.Create(customer.Id, firstCar.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5)));

        await AddRentalAsync(Rental.Create(customer.Id, secondCar.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5)));
    }

    [Fact]
    public async Task OverlappingRentalAfterCancellation_IsAllowed()
    {
        var (customer, car) = await SeedCustomerAndCarAsync();
        var cancelled = Rental.Create(customer.Id, car.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5));
        cancelled.Cancel();
        await AddRentalAsync(cancelled);

        await AddRentalAsync(Rental.Create(customer.Id, car.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5)));
    }

    [Fact]
    public async Task Repository_MapsTheSpecificExclusionViolationToCarNotAvailable()
    {
        var (customer, car) = await SeedCustomerAndCarAsync();
        await AddRentalAsync(Rental.Create(customer.Id, car.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5)));

        await using var context = CreateDbContext();
        var repository = new RentalRepository(context);

        await Assert.ThrowsAsync<CarNotAvailableException>(() => repository.AddAsync(
            Rental.Create(customer.Id, car.Id, new DateOnly(2026, 10, 4), new DateOnly(2026, 10, 10))));
    }

    [Fact]
    public async Task Repository_MapsAnUpdateExclusionViolationToCarNotAvailable()
    {
        var (customer, car) = await SeedCustomerAndCarAsync();
        var rental = Rental.Create(customer.Id, car.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5));
        await AddRentalAsync(rental);
        await AddRentalAsync(Rental.Create(customer.Id, car.Id, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 15)));

        await using var context = CreateDbContext();
        var repository = new RentalRepository(context);
        var persisted = await repository.GetByIdAsync(rental.Id);
        Assert.NotNull(persisted);
        persisted.ChangePeriod(new DateOnly(2026, 10, 8), new DateOnly(2026, 10, 12));

        await Assert.ThrowsAsync<CarNotAvailableException>(() => repository.UpdateAsync(persisted));
    }

    [Fact]
    public async Task CancellingAnActiveRental_ReleasesItsPeriodForANewRental()
    {
        var (customer, car) = await SeedCustomerAndCarAsync();
        var rental = Rental.Create(customer.Id, car.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5));
        await AddRentalAsync(rental);

        await using (var context = CreateDbContext())
        {
            var repository = new RentalRepository(context);
            var persisted = await repository.GetByIdAsync(rental.Id);
            Assert.NotNull(persisted);
            persisted.Cancel();
            await repository.UpdateAsync(persisted);
        }

        await AddRentalAsync(Rental.Create(customer.Id, car.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5)));
    }

    public async ValueTask InitializeAsync()
    {
        await using var context = CreateDbContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private async Task<(Customer Customer, Car FirstCar)> SeedCustomerAndCarAsync()
    {
        var seeded = await SeedAsync(1);
        return (seeded.Customer, seeded.Cars[0]);
    }

    private async Task<(Customer Customer, Car FirstCar, Car SecondCar)> SeedCustomerAndTwoCarsAsync()
    {
        var seeded = await SeedAsync(2);
        return (seeded.Customer, seeded.Cars[0], seeded.Cars[1]);
    }

    private async Task<(Customer Customer, IReadOnlyList<Car> Cars)> SeedAsync(int carCount)
    {
        var customer = new Customer(Guid.NewGuid(), "Ada Lovelace", "Airport Road", "ada@example.com");
        var cars = Enumerable.Range(1, carCount)
            .Select(number => new Car(Guid.NewGuid(), "SUV", $"RAV4-{number}"))
            .ToArray();

        await using var context = CreateDbContext();
        context.Add(customer);
        context.Cars.AddRange(cars);
        await context.SaveChangesAsync();
        return (customer, cars);
    }

    private async Task AddRentalAsync(Rental rental)
    {
        await using var context = CreateDbContext();
        context.Rentals.Add(rental);
        await context.SaveChangesAsync();
    }

    private CarRentalDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<CarRentalDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options);
}

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("car_rental_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public ValueTask InitializeAsync() => new(_container.StartAsync());

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
