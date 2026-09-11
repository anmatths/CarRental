using CarRental.Application.Abstractions;
using CarRental.Application.Dtos;

namespace CarRental.Application.Queries.CheckCarAvailability;

public sealed class CheckCarAvailabilityQueryHandler(ICarRepository carRepository, IRentalRepository rentalRepository)
{
    public async Task<IReadOnlyCollection<AvailableCarDto>> HandleAsync(
        CheckCarAvailabilityQuery query,
        CancellationToken cancellationToken = default)
    {
        var cars = await carRepository.GetByCriteriaAsync(query.Type, query.Model, cancellationToken);
        var availableCars = new List<AvailableCarDto>();

        foreach (var car in cars)
        {
            var activeRentals = await rentalRepository.GetActiveRentalsForCarAsync(
                car.Id, query.StartDate, query.EndDate, cancellationToken);
            if (!activeRentals.Any(rental => rental.Overlaps(query.StartDate, query.EndDate)))
                availableCars.Add(car.ToDto());
        }

        return availableCars;
    }
}
