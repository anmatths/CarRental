namespace CarRental.Application.Commands.RegisterCustomer;

public sealed record RegisterCustomerCommand(string FullName, string Address, string Email);
