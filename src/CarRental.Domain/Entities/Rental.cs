namespace CarRental.Domain.Entities;

public sealed class Rental
{
    public Rental(
        Guid id,
        Guid customerId,
        Guid carId,
        DateOnly startDate,
        DateOnly endDate,
        RentalStatus status)
    {
        Id = id;
        CustomerId = customerId;
        CarId = carId;
        StartDate = startDate;
        EndDate = endDate;
        Status = status;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid CarId { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public RentalStatus Status { get; private set; }
}
