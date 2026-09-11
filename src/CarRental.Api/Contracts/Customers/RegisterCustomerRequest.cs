using System.ComponentModel.DataAnnotations;

namespace CarRental.Api.Contracts.Customers;

public sealed class RegisterCustomerRequest
{
    [Required]
    [StringLength(200)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Address { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; init; } = string.Empty;
}
