using CarRental.Application.Commands.RegisterCustomer;
using CarRental.Application.Queries.CheckCarAvailability;
using Microsoft.Extensions.DependencyInjection;

namespace CarRental.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterCustomerCommandHandler>();
        services.AddScoped<CheckCarAvailabilityQueryHandler>();

        return services;
    }
}
