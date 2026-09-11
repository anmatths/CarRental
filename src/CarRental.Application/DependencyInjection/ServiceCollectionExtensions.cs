using CarRental.Application.Commands.CancelRental;
using CarRental.Application.Commands.ModifyRental;
using CarRental.Application.Commands.RegisterCustomer;
using CarRental.Application.Commands.RegisterRental;
using CarRental.Application.Abstractions;
using CarRental.Application.Queries.CheckCarAvailability;
using Microsoft.Extensions.DependencyInjection;

namespace CarRental.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RegisterCustomerCommandHandler>();
        services.AddScoped<RegisterRentalCommandHandler>();
        services.AddScoped<ModifyRentalCommandHandler>();
        services.AddScoped<CancelRentalCommandHandler>();
        services.AddScoped<CheckCarAvailabilityQueryHandler>();
        services.AddScoped<ICheckCarAvailabilityQueryHandler>(serviceProvider =>
            new CachedCheckCarAvailabilityQueryHandler(
                serviceProvider.GetRequiredService<CheckCarAvailabilityQueryHandler>(),
                serviceProvider.GetRequiredService<IAvailabilityCache>()));

        return services;
    }
}
