namespace CarRental.Domain.Entities;

public sealed class Car
{
    private readonly List<Service> _services = [];

    public Car(Guid id, string type, string model)
    {
        Id = id;
        Type = type;
        Model = model;
    }

    public Guid Id { get; private set; }

    public string Type { get; private set; }

    public string Model { get; private set; }

    public IReadOnlyCollection<Service> Services => _services.AsReadOnly();

    public void AddService(Service service)
    {
        _services.Add(service);
    }
}
