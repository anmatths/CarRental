using CarRental.Application.Abstractions;
using CarRental.Application.Dtos;
using CarRental.Application.Queries.CheckCarAvailability;
using CarRental.Domain.Entities;
using CarRental.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarRental.Application.Tests;

public sealed class AvailabilityCachingTests
{
    private static readonly DateOnly StartDate = new(2026, 10, 1);
    private static readonly DateOnly EndDate = new(2026, 10, 5);

    [Fact]
    public async Task HandleAsync_FirstQueryGetsDataFromSource_AndSecondIdenticalQueryUsesCache()
    {
        var repository = new CountingCarRepository(Car("SUV", "RAV4"));
        var handler = CreateHandler(repository);
        var query = new CheckCarAvailabilityQuery(StartDate, EndDate, "SUV", "RAV4");

        var firstResult = await handler.HandleAsync(query);
        var secondResult = await handler.HandleAsync(query);

        Assert.Single(firstResult);
        Assert.Equal(1, repository.GetByCriteriaCallCount);
        Assert.Same(firstResult, secondResult);
    }

    [Fact]
    public async Task HandleAsync_NormalizesEmptyOptionalFilters()
    {
        var repository = new CountingCarRepository(Car("SUV", "RAV4"));
        var handler = CreateHandler(repository);

        await handler.HandleAsync(new CheckCarAvailabilityQuery(StartDate, EndDate));
        await handler.HandleAsync(new CheckCarAvailabilityQuery(StartDate, EndDate, string.Empty, " "));

        Assert.Equal(1, repository.GetByCriteriaCallCount);
    }

    [Fact]
    public async Task HandleAsync_DifferentTypeUsesDifferentCacheEntry()
    {
        var repository = new CountingCarRepository(Car("SUV", "RAV4"), Car("Sedan", "Corolla"));
        var handler = CreateHandler(repository);

        var suvs = await handler.HandleAsync(new CheckCarAvailabilityQuery(StartDate, EndDate, "SUV"));
        var sedans = await handler.HandleAsync(new CheckCarAvailabilityQuery(StartDate, EndDate, "Sedan"));

        Assert.Equal(2, repository.GetByCriteriaCallCount);
        Assert.NotEqual(suvs.Single().Id, sedans.Single().Id);
    }

    [Fact]
    public async Task HandleAsync_DifferentModelUsesDifferentCacheEntry()
    {
        var repository = new CountingCarRepository(Car("SUV", "RAV4"), Car("SUV", "CR-V"));
        var handler = CreateHandler(repository);

        var rav4 = await handler.HandleAsync(new CheckCarAvailabilityQuery(StartDate, EndDate, Model: "RAV4"));
        var crv = await handler.HandleAsync(new CheckCarAvailabilityQuery(StartDate, EndDate, Model: "CR-V"));

        Assert.Equal(2, repository.GetByCriteriaCallCount);
        Assert.NotEqual(rav4.Single().Id, crv.Single().Id);
    }

    [Fact]
    public async Task HandleAsync_DifferentPeriodUsesDifferentCacheEntry()
    {
        var car = Car("SUV", "RAV4");
        var repository = new CountingCarRepository(car);
        var rentals = new InMemoryRentalRepository(Rental.Create(Guid.NewGuid(), car.Id, StartDate, EndDate));
        var handler = CreateHandler(repository, rentals);

        var beforeRental = await handler.HandleAsync(new CheckCarAvailabilityQuery(new DateOnly(2026, 9, 25), StartDate));
        var duringRental = await handler.HandleAsync(new CheckCarAvailabilityQuery(StartDate, EndDate));

        Assert.Equal(2, repository.GetByCriteriaCallCount);
        Assert.Single(beforeRental);
        Assert.Empty(duringRental);
    }

    [Fact]
    public async Task HandleAsync_CachedResultPreservesAvailableCars()
    {
        var firstCar = Car("SUV", "RAV4");
        var secondCar = Car("SUV", "CR-V");
        var repository = new CountingCarRepository(firstCar, secondCar);
        var handler = CreateHandler(repository);
        var query = new CheckCarAvailabilityQuery(StartDate, EndDate, "SUV");

        await handler.HandleAsync(query);
        var cachedResult = await handler.HandleAsync(query);

        Assert.Equal(2, cachedResult.Count);
        Assert.Contains(cachedResult, car => car.Id == firstCar.Id && car.Model == firstCar.Model);
        Assert.Contains(cachedResult, car => car.Id == secondCar.Id && car.Model == secondCar.Model);
    }

    [Fact]
    public void CreateKey_DifferentParametersProduceDifferentKeys()
    {
        var query = new CheckCarAvailabilityQuery(StartDate, EndDate, "SUV", "RAV4");

        Assert.NotEqual(MemoryAvailabilityCache.CreateKey(query), MemoryAvailabilityCache.CreateKey(query with { Type = "Sedan" }));
        Assert.NotEqual(MemoryAvailabilityCache.CreateKey(query), MemoryAvailabilityCache.CreateKey(query with { Model = "CR-V" }));
        Assert.NotEqual(MemoryAvailabilityCache.CreateKey(query), MemoryAvailabilityCache.CreateKey(query with { EndDate = new DateOnly(2026, 10, 6) }));
    }

    private static CachedCheckCarAvailabilityQueryHandler CreateHandler(
        CountingCarRepository carRepository,
        IRentalRepository? rentalRepository = null)
    {
        var cache = new MemoryAvailabilityCache(new MemoryCache(new MemoryCacheOptions()), NullLogger<MemoryAvailabilityCache>.Instance);
        var sourceHandler = new CheckCarAvailabilityQueryHandler(carRepository, rentalRepository ?? new InMemoryRentalRepository());
        return new CachedCheckCarAvailabilityQueryHandler(sourceHandler, cache);
    }

    private static Car Car(string type, string model) => new(Guid.NewGuid(), type, model);

    private sealed class CountingCarRepository(params Car[] cars) : ICarRepository
    {
        private readonly IReadOnlyCollection<Car> _cars = cars;

        public int GetByCriteriaCallCount { get; private set; }

        public Task<Car?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_cars.SingleOrDefault(car => car.Id == id));

        public Task<IReadOnlyCollection<Car>> GetByCriteriaAsync(string? type, string? model, CancellationToken cancellationToken = default)
        {
            GetByCriteriaCallCount++;
            return Task.FromResult<IReadOnlyCollection<Car>>(_cars.Where(car =>
                (type is null || car.Type == type) &&
                (model is null || car.Model == model)).ToArray());
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

        public Task<IReadOnlyCollection<Rental>> GetActiveRentalsForCarAsync(Guid carId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Rental>>(_rentals.Where(rental => rental.CarId == carId && rental.Overlaps(startDate, endDate)).ToArray());

        public Task<IReadOnlyCollection<Rental>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Rental>>(_rentals.ToArray());

        public Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_rentals.SingleOrDefault(rental => rental.Id == id));

        public Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
