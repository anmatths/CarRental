using CarRental.Application.Abstractions;
using CarRental.Application.Dtos;
using CarRental.Domain.Entities;

namespace CarRental.Application.Commands.RegisterCustomer;

public sealed class RegisterCustomerCommandHandler(ICustomerRepository customerRepository)
{
    public async Task<CustomerDto> HandleAsync(RegisterCustomerCommand command, CancellationToken cancellationToken = default)
    {
        var customer = new Customer(Guid.NewGuid(), command.FullName, command.Address, command.Email);
        await customerRepository.AddAsync(customer, cancellationToken);
        return customer.ToDto();
    }
}
