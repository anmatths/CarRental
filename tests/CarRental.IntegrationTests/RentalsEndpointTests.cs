using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CarRental.Domain.Entities;
using CarRental.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarRental.IntegrationTests;

public sealed class RentalsEndpointTests : IClassFixture<RentalsApiFactory>, IAsyncLifetime
{
    private readonly RentalsApiFactory _factory;
    private readonly HttpClient _client;

    public RentalsEndpointTests(RentalsApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_WhenCustomerAndCarExistAndCarIsAvailable_CreatesActiveRental()
    {
        var customer = new Customer(Guid.NewGuid(), "Ada Lovelace", "Airport Road", "ada@example.com");
        var car = new Car(Guid.NewGuid(), "SUV", "RAV4");
        await SeedAsync(customer, car);

        var response = await _client.PostAsJsonAsync("/api/rentals", new
        {
            customerId = customer.Id,
            carId = car.Id,
            startDate = "2026-10-01",
            endDate = "2026-10-10"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var rental = await response.Content.ReadFromJsonAsync<RentalResponse>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        });

        Assert.NotNull(rental);
        Assert.Equal(customer.Id, rental.CustomerId);
        Assert.Equal(car.Id, rental.CarId);
        Assert.Equal(RentalStatus.Active, rental.Status);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CarRentalDbContext>();
        var persisted = await dbContext.Rentals.SingleAsync(entity => entity.Id == rental.Id);
        Assert.Equal(RentalStatus.Active, persisted.Status);
    }

    [Fact]
    public async Task Post_WithInvalidPeriod_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/rentals", new
        {
            customerId = Guid.NewGuid(),
            carId = Guid.NewGuid(),
            startDate = "2026-10-10",
            endDate = "2026-10-10"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_WhenRentalExists_ChangesItsPeriodAndReturnsTheUpdatedRental()
    {
        var customer = new Customer(Guid.NewGuid(), "Ada Lovelace", "Airport Road", "ada@example.com");
        var car = new Car(Guid.NewGuid(), "SUV", "RAV4");
        var rental = Rental.Create(customer.Id, car.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5));
        await SeedAsync(customer, car, rental);

        var response = await _client.PutAsJsonAsync($"/api/rentals/{rental.Id}", new
        {
            startDate = "2026-10-05",
            endDate = "2026-10-10"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RentalResponse>(new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        });
        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2026, 10, 5), result.StartDate);
        Assert.Equal(new DateOnly(2026, 10, 10), result.EndDate);
    }

    [Fact]
    public async Task Put_WithInvalidPeriod_ReturnsBadRequest()
    {
        var response = await _client.PutAsJsonAsync($"/api/rentals/{Guid.NewGuid()}", new
        {
            startDate = "2026-10-10",
            endDate = "2026-10-10"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_CancelsButKeepsTheRentalAndReleasesAvailability()
    {
        var customer = new Customer(Guid.NewGuid(), "Ada Lovelace", "Airport Road", "ada@example.com");
        var car = new Car(Guid.NewGuid(), "SUV", "RAV4");
        var rental = Rental.Create(customer.Id, car.Id, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 10));
        await SeedAsync(customer, car, rental);

        var response = await _client.DeleteAsync($"/api/rentals/{rental.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CarRentalDbContext>();
            var persisted = await dbContext.Rentals.SingleAsync(entity => entity.Id == rental.Id);
            Assert.Equal(RentalStatus.Cancelled, persisted.Status);
        }

        var availability = await _client.GetFromJsonAsync<List<AvailableCarResponse>>(
            "/api/cars/availability?startDate=2026-10-01&endDate=2026-10-10");
        Assert.Contains(availability!, availableCar => availableCar.Id == car.Id);
    }

    private async Task SeedAsync(Customer customer, Car car, params Rental[] rentals)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CarRentalDbContext>();
        dbContext.AddRange(customer, car);
        dbContext.Rentals.AddRange(rentals);
        await dbContext.SaveChangesAsync();
    }

    public async Task InitializeAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CarRentalDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record RentalResponse(Guid Id, Guid CustomerId, Guid CarId, DateOnly StartDate, DateOnly EndDate, RentalStatus Status);

    private sealed record AvailableCarResponse(Guid Id, string Type, string Model);
}

public sealed class RentalsApiFactory : WebApplicationFactory<Program>
{
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<CarRentalDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<CarRentalDbContext>));
            services.AddDbContext<CarRentalDbContext>(options =>
                options.UseInMemoryDatabase("rentals-tests", _databaseRoot));
        });
    }
}
