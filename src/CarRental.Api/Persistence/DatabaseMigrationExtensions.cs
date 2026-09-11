using CarRental.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Api.Persistence;

internal static class DatabaseMigrationExtensions
{
    /// <summary>
    /// Applies pending migrations and demo data to make the local Development/Docker challenge environment ready to use.
    /// Production deployments should apply migrations through their deployment process instead.
    /// </summary>
    internal static async Task ApplyDevelopmentMigrationsAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CarRentalDbContext>();
        await dbContext.Database.MigrateAsync();
        await DevelopmentDataSeeder.SeedAsync(dbContext);
    }
}
