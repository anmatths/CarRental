namespace CarRental.Domain.Entities;

public sealed class Customer
{
    public Customer(Guid id, string fullName, string address, string email)
    {
        Id = id;
        FullName = fullName;
        Address = address;
        Email = email;
    }

    public Guid Id { get; private set; }

    public string FullName { get; private set; }

    public string Address { get; private set; }

    public string Email { get; private set; }
}
