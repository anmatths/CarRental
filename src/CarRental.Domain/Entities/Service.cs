namespace CarRental.Domain.Entities;

public sealed class Service
{
    public Service(Guid id, DateOnly date)
    {
        Id = id;
        Date = date;
    }

    public Guid Id { get; private set; }

    public DateOnly Date { get; private set; }
}
