using CarRental.Application.Abstractions;
using CarRental.Infrastructure.Caching;
using CarRental.Infrastructure.Persistence;
using CarRental.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarRental.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CarRental");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'CarRental' must be configured.");
        }

        services.AddDbContext<CarRentalDbContext>(options => options.UseNpgsql(connectionString));
        services.AddMemoryCache();
        services.AddSingleton<IAvailabilityCache, MemoryAvailabilityCache>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICarRepository, CarRepository>();
        services.AddScoped<IRentalRepository, RentalRepository>();

        return services;
    }
}
