using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CarRental.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.FullName).HasMaxLength(200).IsRequired();
        builder.Property(customer => customer.Address).HasMaxLength(500).IsRequired();
        builder.Property(customer => customer.Email).HasMaxLength(320).IsRequired();
    }
}
