using CarRental.Application.Abstractions;
using CarRental.Application.Queries.GetRentals;
using CarRental.Domain.Entities;

namespace CarRental.Application.Tests;

public sealed class GetRentalsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsRentalsFromTheRepository()
    {
        var active = Rental.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5));
        var cancelled = Rental.Create(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 10, 6), new DateOnly(2026, 10, 10));
        cancelled.Cancel();
        var handler = new GetRentalsQueryHandler(new RentalRepository(active, cancelled));

        var result = await handler.HandleAsync(new GetRentalsQuery());

        Assert.Collection(result,
            rental => Assert.Equal(active.Id, rental.Id),
            rental =>
            {
                Assert.Equal(cancelled.Id, rental.Id);
                Assert.Equal(RentalStatus.Cancelled, rental.Status);
            });
    }

    [Fact]
    public async Task HandleAsync_WhenThereAreNoRentals_ReturnsAnEmptyCollection()
    {
        var handler = new GetRentalsQueryHandler(new RentalRepository());

        var result = await handler.HandleAsync(new GetRentalsQuery());

        Assert.Empty(result);
    }

    private sealed class RentalRepository(params Rental[] rentals) : IRentalRepository
    {
        public Task<Rental?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Rental?>(null);
        public Task<IReadOnlyCollection<Rental>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Rental>>(rentals);
        public Task<IReadOnlyCollection<Rental>> GetActiveRentalsForCarAsync(Guid carId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Rental>>([]);
        public Task AddAsync(Rental rental, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(Rental rental, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
