using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public sealed class RentalConfiguration : IEntityTypeConfiguration<Rental>
{
    public void Configure(EntityTypeBuilder<Rental> builder)
    {
        builder.ToTable("Rentals");
        builder.HasKey(rental => rental.Id);
        builder.Property(rental => rental.StartDate).HasColumnType("date").IsRequired();
        builder.Property(rental => rental.EndDate).HasColumnType("date").IsRequired();
        builder.Property(rental => rental.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(rental => rental.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Car>()
            .WithMany()
            .HasForeignKey(rental => rental.CarId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(rental => new { rental.CarId, rental.Status, rental.StartDate, rental.EndDate });
    }
}
