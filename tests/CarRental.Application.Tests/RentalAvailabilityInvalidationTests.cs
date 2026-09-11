using CarRental.Application.Abstractions;
using CarRental.Application.Commands.CancelRental;
using CarRental.Application.Commands.ModifyRental;
using CarRental.Application.Commands.RegisterRental;
using CarRental.Application.Exceptions;
using CarRental.Application.Queries.CheckCarAvailability;
using CarRental.Domain.Entities;
using CarRental.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarRental.Application.Tests;

public sealed class RentalAvailabilityInvalidationTests
{
    private static readonly DateOnly OriginalStart = new(2026, 10, 1);
    private static readonly DateOnly OriginalEnd = new(2026, 10, 5);
    private static readonly DateOnly NewStart = new(2026, 10, 10);
    private static readonly DateOnly NewEnd = new(2026, 10, 15);

    [Fact]
    public async Task RegisterRental_InvalidatesCachedAvailabilityAfterPersistence()
    {
        var customer = Customer();
        var car = Car();
        var rentals = new InMemoryRentalRepository();
        var cache = CreateCache();
        var availability = CreateAvailabilityHandler(car, rentals, cache);
        var query = new CheckCarAvailabilityQuery(OriginalStart, OriginalEnd);

        Assert.Single(await availability.HandleAsync(query));

        await new RegisterRentalCommandHandler(new CustomerRepository(customer), new CarRepository(car), rentals, cache)
            .HandleAsync(new RegisterRentalCommand(customer.Id, car.Id, OriginalStart, OriginalEnd));

        Assert.Empty(await availability.HandleAsync(query));
    }

    [Fact]
    public async Task CancelRental_InvalidatesCachedAvailabilityAfterPersistence()
    {
        var car = Car();
        var rental = Rental.Create(Guid.NewGuid(), car.Id, OriginalStart, OriginalEnd);
        var rentals = new InMemoryRentalRepository(rental);
        var cache = CreateCache();
        var availability = CreateAvailabilityHandler(car, rentals, cache);
        var query = new CheckCarAvailabilityQuery(OriginalStart, OriginalEnd);

        Assert.Empty(await availability.HandleAsync(query));

        await new CancelRentalCommandHandler(rentals, cache).HandleAsync(new CancelRentalCommand(rental.Id));

        Assert.Single(await availability.HandleAsync(query));
    }

    [Fact]
    public async Task ModifyRental_InvalidatesBothTheOldAndNewPeriods()
    {
        var car = Car();
        var rental = Rental.Create(Guid.NewGuid(), car.Id, OriginalStart, OriginalEnd);
        var rentals = new InMemoryRentalRepository(rental);
        var cache = CreateCache();
        var availability = CreateAvailabilityHandler(car, rentals, cache);
        var oldPeriod = new CheckCarAvailabilityQuery(OriginalStart, OriginalEnd);
        var newPeriod = new CheckCarAvailabilityQuery(NewStart, NewEnd);

        Assert.Empty(await availability.HandleAsync(oldPeriod));
        Assert.Single(await availability.HandleAsync(newPeriod));

        await new ModifyRentalCommandHandler(rentals, cache)
            .HandleAsync(new ModifyRentalCommand(rental.Id, NewStart, NewEnd));

        Assert.Single(await availability.HandleAsync(oldPeriod));
        Assert.Empty(await availability.HandleAsync(newPeriod));
    }

    [Fact]
    public async Task FailedRentalWrite_DoesNotInvalidateAvailability()
    {
        var customer = Customer();
        var car = Car();
        var rentals = new InMemoryRentalRepository(Rental.Create(customer.Id, car.Id, OriginalStart, OriginalEnd));
        var cache = CreateCache();
        var cars = new CarRepository(car);
        var availability = new CachedCheckCarAvailabilityQueryHandler(
            new CheckCarAvailabilityQueryHandler(cars, rentals), cache);
        var query = new CheckCarAvailabilityQuery(OriginalStart, OriginalEnd);

        await availability.HandleAsync(query);
        await Assert.ThrowsAsync<CarNotAvailableException>(() =>
            new RegisterRentalCommandHandler(new CustomerRepository(customer), new CarRepository(car), rentals, cache)
                .HandleAsync(new RegisterRentalCommand(customer.Id, car.Id, OriginalStart, OriginalEnd)));

        await availability.HandleAsync(query);
        Assert.Equal(1, cars.GetByCriteriaCallCount);
    }

    [Fact]
    public void Invalidate_AdvancesTheGenerationUsedByNewQueries()
    {
        var cache = CreateCache();
        var query = new CheckCarAvailabilityQuery(OriginalStart, OriginalEnd, "SUV", "RAV4");
        var before = cache.Generation;

        cache.Invalidate();

        Assert.Equal(before + 1, cache.Generation);
        Assert.NotEqual(
            MemoryAvailabilityCache.CreateKey(query, before),
            MemoryAvailabilityCache.CreateKey(query, cache.Generation));
    }

    private static CachedCheckCarAvailabilityQueryHandler CreateAvailabilityHandler(
        Car car,
        InMemoryRentalRepository rentals,
        MemoryAvailabilityCache cache) =>
        new(new CheckCarAvailabilityQueryHandler(new CarRepository(car), rentals), cache);

    private static MemoryAvailabilityCache CreateCache() =>
        new(new MemoryCache(new MemoryCacheOptions()), NullLogger<MemoryAvailabilityCache>.Instance);

    private static Customer Customer() => new(Guid.NewGuid(), "Ada Lovelace", "Airport Road", "ada@example.com");

    private static Car Car() => new(Guid.NewGuid(), "SUV", "RAV4");

    private sealed class CustomerRepository(params Customer[] customers) : ICustomerRepository
    {
        public Task AddAsync(Customer customer, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(customers.SingleOrDefault(customer => customer.Id == id));
    }

    private sealed class CarRepository(params Car[] cars) : ICarRepository
    {
        public int GetByCriteriaCallCount { get; private set; }

        public Task<Car?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(cars.SingleOrDefault(car => car.Id == id));

        public Task<IReadOnlyCollection<Car>> GetByCriteriaAsync(string? type, string? model, CancellationToken cancellationToken = default)
        {
            GetByCriteriaCallCount++;
            return Task.FromResult<IReadOnlyCollection<Car>>(cars.Where(car =>
                (type is null || car.Type == type) && (model is null || car.Model == model)).ToArray());
        }
    }

    private sealed class InMemoryRentalRepository(params Rental[] rentals) : IRentalRepository
    {
        private readonly List<Rental> _rentals = [.. rentals];

        public Task AddAsync(Rental rental, CancellationToken cancellationToken = default)
        {
            _rentals.Add(rental);
            return Task.CompletedTask;
        }

        public Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_rentals.SingleOrDefault(rental => rental.Id == id));

        public Task<IReadOnlyCollection<Rental>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Rental>>(_rentals.ToArray());

        public Task<IReadOnlyCollection<Rental>> GetActiveRentalsForCarAsync(Guid carId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Rental>>(_rentals.Where(rental =>
                rental.CarId == carId && rental.Status == RentalStatus.Active && rental.Overlaps(startDate, endDate)).ToArray());

        public Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
