using CarRental.Application.Abstractions;
using CarRental.Application.Commands.CancelRental;
using CarRental.Application.Commands.ModifyRental;
using CarRental.Application.Commands.RegisterRental;
using CarRental.Application.Exceptions;
using CarRental.Application.Queries.CheckCarAvailability;
using CarRental.Domain.Entities;
using CarRental.Domain.Exceptions;

namespace CarRental.Application.Tests;

public sealed class HandlersTests
{
    private static readonly DateOnly StartDate = new(2026, 10, 1);
    private static readonly DateOnly EndDate = new(2026, 10, 5);

    [Fact]
    public async Task RegisterRental_WhenCarIsAvailable_AddsRental()
    {
        var customer = Customer();
        var car = Car();
        var rentals = new RentalRepository();
        var handler = new RegisterRentalCommandHandler(new CustomerRepository(customer), new CarRepository(car), rentals);

        var result = await handler.HandleAsync(new RegisterRentalCommand(customer.Id, car.Id, StartDate, EndDate));

        Assert.Equal(RentalStatus.Active, result.Status);
        Assert.Single(rentals.Rentals);
    }

    [Fact]
    public async Task RegisterRental_WhenCustomerDoesNotExist_Throws()
    {
        var car = Car();
        var handler = new RegisterRentalCommandHandler(new CustomerRepository(), new CarRepository(car), new RentalRepository());

        await Assert.ThrowsAsync<CustomerNotFoundException>(
            () => handler.HandleAsync(new RegisterRentalCommand(Guid.NewGuid(), car.Id, StartDate, EndDate)));
    }

    [Fact]
    public async Task RegisterRental_WhenCarDoesNotExist_Throws()
    {
        var customer = Customer();
        var handler = new RegisterRentalCommandHandler(new CustomerRepository(customer), new CarRepository(), new RentalRepository());

        await Assert.ThrowsAsync<CarNotFoundException>(
            () => handler.HandleAsync(new RegisterRentalCommand(customer.Id, Guid.NewGuid(), StartDate, EndDate)));
    }

    [Fact]
    public async Task RegisterRental_WhenPeriodOverlaps_Throws()
    {
        var customer = Customer();
        var car = Car();
        var rentals = new RentalRepository(Rental.Create(customer.Id, car.Id, StartDate, EndDate));
        var handler = new RegisterRentalCommandHandler(new CustomerRepository(customer), new CarRepository(car), rentals);

        await Assert.ThrowsAsync<CarNotAvailableException>(
            () => handler.HandleAsync(new RegisterRentalCommand(customer.Id, car.Id, new DateOnly(2026, 10, 3), new DateOnly(2026, 10, 7))));
    }

    [Fact]
    public async Task RegisterRental_WhenPeriodIsInvalid_Throws()
    {
        var customer = Customer();
        var car = Car();
        var handler = new RegisterRentalCommandHandler(new CustomerRepository(customer), new CarRepository(car), new RentalRepository());

        await Assert.ThrowsAsync<InvalidRentalPeriodException>(
            () => handler.HandleAsync(new RegisterRentalCommand(customer.Id, car.Id, EndDate, StartDate)));
    }

    [Fact]
    public async Task RegisterRental_WhenPeriodIsAdjacent_AddsRental()
    {
        var customer = Customer();
        var car = Car();
        var rentals = new RentalRepository(Rental.Create(customer.Id, car.Id, StartDate, EndDate));
        var handler = new RegisterRentalCommandHandler(new CustomerRepository(customer), new CarRepository(car), rentals);

        await handler.HandleAsync(new RegisterRentalCommand(customer.Id, car.Id, EndDate, new DateOnly(2026, 10, 10)));

        Assert.Equal(2, rentals.Rentals.Count);
    }

    [Fact]
    public async Task RegisterRental_WhenOnlyConflictIsCancelled_AddsRental()
    {
        var customer = Customer();
        var car = Car();
        var cancelled = Rental.Create(customer.Id, car.Id, StartDate, EndDate);
        cancelled.Cancel();
        var rentals = new RentalRepository(cancelled);
        var handler = new RegisterRentalCommandHandler(new CustomerRepository(customer), new CarRepository(car), rentals);

        await handler.HandleAsync(new RegisterRentalCommand(customer.Id, car.Id, StartDate, EndDate));

        Assert.Equal(2, rentals.Rentals.Count);
    }

    [Fact]
    public async Task ModifyRental_WhenValid_ChangesPeriod()
    {
        var rental = Rental.Create(Guid.NewGuid(), Guid.NewGuid(), StartDate, EndDate);
        var handler = new ModifyRentalCommandHandler(new RentalRepository(rental));
        var newStart = new DateOnly(2026, 10, 5);
        var newEnd = new DateOnly(2026, 10, 10);

        var result = await handler.HandleAsync(new ModifyRentalCommand(rental.Id, newStart, newEnd));

        Assert.Equal(newStart, result.StartDate);
        Assert.Equal(newEnd, result.EndDate);
    }

    [Fact]
    public async Task ModifyRental_WhenItConflictsWithAnotherRental_Throws()
    {
        var carId = Guid.NewGuid();
        var rental = Rental.Create(Guid.NewGuid(), carId, StartDate, EndDate);
        var other = Rental.Create(Guid.NewGuid(), carId, new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 15));
        var handler = new ModifyRentalCommandHandler(new RentalRepository(rental, other));

        await Assert.ThrowsAsync<CarNotAvailableException>(
            () => handler.HandleAsync(new ModifyRentalCommand(rental.Id, new DateOnly(2026, 10, 8), new DateOnly(2026, 10, 12))));
    }

    [Fact]
    public async Task ModifyRental_DoesNotTreatItselfAsAConflict()
    {
        var rental = Rental.Create(Guid.NewGuid(), Guid.NewGuid(), StartDate, EndDate);
        var handler = new ModifyRentalCommandHandler(new RentalRepository(rental));

        var result = await handler.HandleAsync(new ModifyRentalCommand(rental.Id, StartDate, EndDate));

        Assert.Equal(rental.Id, result.Id);
    }

    [Fact]
    public async Task CancelRental_WhenExisting_CancelsIt()
    {
        var rental = Rental.Create(Guid.NewGuid(), Guid.NewGuid(), StartDate, EndDate);
        var handler = new CancelRentalCommandHandler(new RentalRepository(rental));

        var result = await handler.HandleAsync(new CancelRentalCommand(rental.Id));

        Assert.Equal(RentalStatus.Cancelled, result.Status);
    }

    [Fact]
    public async Task CancelRental_WhenMissing_Throws()
    {
        var handler = new CancelRentalCommandHandler(new RentalRepository());

        await Assert.ThrowsAsync<RentalNotFoundException>(() => handler.HandleAsync(new CancelRentalCommand(Guid.NewGuid())));
    }

    [Fact]
    public async Task CheckAvailability_ReturnsOnlyCarsWithoutConflictsAndHonorsFilters()
    {
        var available = Car(type: "SUV", model: "Rav4");
        var occupied = Car(type: "SUV", model: "Rav4");
        var differentType = Car(type: "Sedan", model: "Rav4");
        var rental = Rental.Create(Guid.NewGuid(), occupied.Id, StartDate, EndDate);
        var handler = new CheckCarAvailabilityQueryHandler(
            new CarRepository(available, occupied, differentType),
            new RentalRepository(rental));

        var result = await handler.HandleAsync(new CheckCarAvailabilityQuery(StartDate, EndDate, "SUV", "Rav4"));

        var car = Assert.Single(result);
        Assert.Equal(available.Id, car.Id);
    }

    [Fact]
    public async Task CheckAvailability_AllowsAdjacentRentals()
    {
        var car = Car();
        var rental = Rental.Create(Guid.NewGuid(), car.Id, StartDate, EndDate);
        var handler = new CheckCarAvailabilityQueryHandler(new CarRepository(car), new RentalRepository(rental));

        var result = await handler.HandleAsync(new CheckCarAvailabilityQuery(EndDate, new DateOnly(2026, 10, 10)));

        Assert.Single(result);
    }

    private static Customer Customer() => new(Guid.NewGuid(), "Ada Lovelace", "Airport Road", "ada@example.com");

    private static Car Car(string type = "SUV", string model = "Rav4") => new(Guid.NewGuid(), type, model);

    private sealed class CustomerRepository(params Customer[] customers) : ICustomerRepository
    {
        private readonly List<Customer> _customers = [.. customers];

        public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            _customers.Add(customer);
            return Task.CompletedTask;
        }

        public Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_customers.SingleOrDefault(customer => customer.Id == id));
    }

    private sealed class CarRepository(params Car[] cars) : ICarRepository
    {
        private readonly List<Car> _cars = [.. cars];

        public Task<Car?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_cars.SingleOrDefault(car => car.Id == id));

        public Task<IReadOnlyCollection<Car>> GetByCriteriaAsync(string? type, string? model, CancellationToken cancellationToken = default)
        {
            var cars = _cars.Where(car =>
                (type is null || car.Type == type) &&
                (model is null || car.Model == model)).ToArray();
            return Task.FromResult<IReadOnlyCollection<Car>>(cars);
        }
    }

    private sealed class RentalRepository(params Rental[] rentals) : IRentalRepository
    {
        public List<Rental> Rentals { get; } = [.. rentals];

        public Task AddAsync(Rental rental, CancellationToken cancellationToken = default)
        {
            Rentals.Add(rental);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<Rental>> GetActiveRentalsForCarAsync(
            Guid carId,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Rental>>(Rentals
                .Where(rental => rental.CarId == carId && rental.Overlaps(startDate, endDate)).ToArray());

        public Task<IReadOnlyCollection<Rental>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Rental>>(Rentals.ToArray());

        public Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Rentals.SingleOrDefault(rental => rental.Id == id));

        public Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
