using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public sealed class CarConfiguration : IEntityTypeConfiguration<Car>
{
    public void Configure(EntityTypeBuilder<Car> builder)
    {
        builder.ToTable("Cars");
        builder.HasKey(car => car.Id);
        builder.Property(car => car.Type).HasMaxLength(100).IsRequired();
        builder.Property(car => car.Model).HasMaxLength(150).IsRequired();

        builder.HasMany(car => car.Services)
            .WithOne()
            .HasForeignKey("CarId")
            .IsRequired();

        builder.Navigation(car => car.Services)
            .HasField("_services")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
