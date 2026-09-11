using CarRental.Domain.Exceptions;

namespace CarRental.Domain.Entities;

public sealed class Rental
{
    private Rental(Guid id, Guid customerId, Guid carId, DateOnly startDate, DateOnly endDate)
    {
        Id = id;
        CustomerId = customerId;
        CarId = carId;
        StartDate = startDate;
        EndDate = endDate;
        Status = RentalStatus.Active;
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public Guid CarId { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public RentalStatus Status { get; private set; }

    public static Rental Create(Guid customerId, Guid carId, DateOnly startDate, DateOnly endDate)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainValidationException("A rental must have a valid customer identifier.");
        }

        if (carId == Guid.Empty)
        {
            throw new DomainValidationException("A rental must have a valid car identifier.");
        }

        ValidatePeriod(startDate, endDate);

        return new Rental(Guid.NewGuid(), customerId, carId, startDate, endDate);
    }

    /// <summary>
    /// Determines whether this active rental overlaps the supplied half-open period [startDate, endDate).
    /// The start date is inclusive and the end date is exclusive, so adjacent periods do not overlap.
    /// </summary>
    public bool Overlaps(DateOnly startDate, DateOnly endDate)
    {
        ValidatePeriod(startDate, endDate);

        return Status != RentalStatus.Cancelled
            && StartDate < endDate
            && EndDate > startDate;
    }

    public void Cancel()
    {
        Status = RentalStatus.Cancelled;
    }

    public void ChangePeriod(DateOnly startDate, DateOnly endDate)
    {
        if (Status == RentalStatus.Cancelled)
        {
            throw new InvalidRentalOperationException("A cancelled rental cannot have its period changed.");
        }

        ValidatePeriod(startDate, endDate);

        StartDate = startDate;
        EndDate = endDate;
    }

    private static void ValidatePeriod(DateOnly startDate, DateOnly endDate)
    {
        if (endDate <= startDate)
        {
            throw new InvalidRentalPeriodException("The rental end date must be later than the start date.");
        }
    }
}
