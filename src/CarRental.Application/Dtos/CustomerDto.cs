namespace CarRental.Application.Dtos;

public sealed record CustomerDto(Guid Id, string FullName, string Address, string Email);
