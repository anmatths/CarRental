using System.Net;
using System.Net.Http.Json;
using CarRental.Domain.Entities;
using CarRental.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarRental.IntegrationTests;

public sealed class CarsAvailabilityEndpointTests : IClassFixture<CarsApiFactory>, IAsyncLifetime
{
    private readonly CarsApiFactory _factory;
    private readonly HttpClient _client;

    public CarsAvailabilityEndpointTests(CarsApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Get_ReturnsAvailableCars_AndExcludesOverlappingRentals()
    {
        var availableCar = await SeedCarAsync("SUV", "RAV4");
        await SeedCarWithRentalAsync("SUV", "CR-V", new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 15));

        var cars = await GetCarsAsync("2026-10-12", "2026-10-18");

        Assert.Single(cars);
        Assert.Equal(availableCar.Id, cars[0].Id);
    }

    [Theory]
    [InlineData("2026-10-15", "2026-10-20")]
    [InlineData("2026-10-05", "2026-10-10")]
    public async Task Get_IncludesCarsWithAdjacentRentals(string startDate, string endDate)
    {
        var car = await SeedCarWithRentalAsync("SUV", "RAV4", new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 15));

        var cars = await GetCarsAsync(startDate, endDate);

        Assert.Contains(cars, result => result.Id == car.Id);
    }

    [Fact]
    public async Task Get_IgnoresCancelledRentals()
    {
        var car = await SeedCarWithRentalAsync("SUV", "RAV4", new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 15), cancelled: true);

        var cars = await GetCarsAsync("2026-10-12", "2026-10-14");

        Assert.Contains(cars, result => result.Id == car.Id);
    }

    [Fact]
    public async Task Get_AppliesTypeFilter()
    {
        var expected = await SeedCarAsync("SUV", "RAV4");
        await SeedCarAsync("Sedan", "Corolla");

        var cars = await GetCarsAsync("2026-10-01", "2026-10-10", "SUV");

        Assert.Single(cars);
        Assert.Equal(expected.Id, cars[0].Id);
    }

    [Fact]
    public async Task Get_AppliesModelFilter()
    {
        var expected = await SeedCarAsync("SUV", "RAV4");
        await SeedCarAsync("SUV", "CR-V");

        var cars = await GetCarsAsync("2026-10-01", "2026-10-10", model: "RAV4");

        Assert.Single(cars);
        Assert.Equal(expected.Id, cars[0].Id);
    }

    [Theory]
    [InlineData("2026-10-10", "2026-10-10")]
    [InlineData("2026-10-10", "2026-10-05")]
    public async Task Get_WithInvalidPeriod_ReturnsBadRequest(string startDate, string endDate)
    {
        var response = await _client.GetAsync($"/api/cars/availability?startDate={startDate}&endDate={endDate}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/cars/availability?endDate=2026-10-10")]
    [InlineData("/api/cars/availability?startDate=2026-10-01")]
    public async Task Get_WithMissingRequiredDate_ReturnsBadRequest(string path)
    {
        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenNoCarsAreAvailable_ReturnsEmptyCollection()
    {
        await SeedCarWithRentalAsync("SUV", "RAV4", new DateOnly(2026, 10, 10), new DateOnly(2026, 10, 15));

        var cars = await GetCarsAsync("2026-10-12", "2026-10-14");

        Assert.Empty(cars);
    }

    private async Task<Car> SeedCarAsync(string type, string model)
    {
        var car = new Car(Guid.NewGuid(), type, model);
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CarRentalDbContext>();
        dbContext.Cars.Add(car);
        await dbContext.SaveChangesAsync();
        return car;
    }

    private async Task<Car> SeedCarWithRentalAsync(string type, string model, DateOnly startDate, DateOnly endDate, bool cancelled = false)
    {
        var car = await SeedCarAsync(type, model);
        var rental = Rental.Create(Guid.NewGuid(), car.Id, startDate, endDate);
        if (cancelled)
        {
            rental.Cancel();
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CarRentalDbContext>();
        dbContext.Rentals.Add(rental);
        await dbContext.SaveChangesAsync();
        return car;
    }

    private async Task<IReadOnlyList<AvailableCarResponse>> GetCarsAsync(string startDate, string endDate, string? type = null, string? model = null)
    {
        var path = $"/api/cars/availability?startDate={startDate}&endDate={endDate}";
        if (type is not null)
        {
            path += $"&type={Uri.EscapeDataString(type)}";
        }

        if (model is not null)
        {
            path += $"&model={Uri.EscapeDataString(model)}";
        }

        var response = await _client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AvailableCarResponse>>() ?? [];
    }

    private sealed record AvailableCarResponse(Guid Id, string Type, string Model);

    public async Task InitializeAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CarRentalDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
        ((MemoryCache)scope.ServiceProvider.GetRequiredService<IMemoryCache>()).Compact(1.0);
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

public sealed class CarsApiFactory : WebApplicationFactory<Program>
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
                options.UseInMemoryDatabase("cars-tests", _databaseRoot));
        });
    }
}
