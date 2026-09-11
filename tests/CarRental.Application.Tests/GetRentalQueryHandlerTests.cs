using CarRental.Application.Abstractions;
using CarRental.Application.Exceptions;
using CarRental.Application.Queries.GetRental;
using CarRental.Domain.Entities;

namespace CarRental.Application.Tests;

public sealed class GetRentalQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenRentalExists_ReturnsItsDto()
    {
        var rental = Rental.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5));
        var handler = new GetRentalQueryHandler(new RentalRepository(rental));

        var result = await handler.HandleAsync(new GetRentalQuery(rental.Id));

        Assert.Equal(rental.Id, result.Id);
        Assert.Equal(rental.CustomerId, result.CustomerId);
        Assert.Equal(rental.CarId, result.CarId);
        Assert.Equal(rental.StartDate, result.StartDate);
        Assert.Equal(rental.EndDate, result.EndDate);
        Assert.Equal(RentalStatus.Active, result.Status);
    }

    [Fact]
    public async Task HandleAsync_WhenRentalDoesNotExist_ThrowsRentalNotFoundException()
    {
        var handler = new GetRentalQueryHandler(new RentalRepository());

        await Assert.ThrowsAsync<RentalNotFoundException>(() => handler.HandleAsync(new GetRentalQuery(Guid.NewGuid())));
    }

    private sealed class RentalRepository(params Rental[] rentals) : IRentalRepository
    {
        public Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(rentals.SingleOrDefault(rental => rental.Id == id));

        public Task<IReadOnlyCollection<Rental>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Rental>>(rentals);

        public Task<IReadOnlyCollection<Rental>> GetActiveRentalsForCarAsync(Guid carId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<Rental>>([]);

        public Task AddAsync(Rental rental, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
