using CarRental.Application.Dtos;

namespace CarRental.Api.Contracts.Customers;

public sealed record CustomerResponse(Guid Id, string FullName, string Address, string Email)
{
    public static CustomerResponse From(CustomerDto customer) =>
        new(customer.Id, customer.FullName, customer.Address, customer.Email);
}
