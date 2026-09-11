using System.Net;
using System.Net.Http.Json;
using CarRental.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarRental.IntegrationTests;

public sealed class CustomersEndpointTests : IClassFixture<CustomersApiFactory>
{
    private readonly HttpClient _client;

    public CustomersEndpointTests(CustomersApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Post_WithValidRequest_ReturnsCreatedCustomer()
    {
        var request = new { fullName = "John Doe", address = "123 Main Street", email = "john@example.com" };

        var response = await _client.PostAsJsonAsync("/api/customers", request);
        var customer = await response.Content.ReadFromJsonAsync<CustomerResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(customer);
        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.Equal(request.fullName, customer.FullName);
        Assert.Equal(request.address, customer.Address);
        Assert.Equal(request.email, customer.Email);
    }

    [Theory]
    [InlineData("", "123 Main Street", "john@example.com")]
    [InlineData("John Doe", "", "john@example.com")]
    [InlineData("John Doe", "123 Main Street", "not-an-email")]
    public async Task Post_WithInvalidRequest_ReturnsBadRequest(string fullName, string address, string email)
    {
        var response = await _client.PostAsJsonAsync("/api/customers", new { fullName, address, email });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private sealed record CustomerResponse(Guid Id, string FullName, string Address, string Email);
}

public sealed class CustomersApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<CarRentalDbContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<CarRentalDbContext>));

            services.AddDbContext<CarRentalDbContext>(options =>
                options.UseInMemoryDatabase($"customers-tests-{Guid.NewGuid()}"));
        });
    }
}
