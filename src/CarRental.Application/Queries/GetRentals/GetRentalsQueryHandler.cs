using CarRental.Application.Abstractions;
using CarRental.Application.Dtos;

namespace CarRental.Application.Queries.GetRentals;

public sealed class GetRentalsQueryHandler(IRentalRepository rentalRepository)
{
    public async Task<IReadOnlyCollection<RentalDto>> HandleAsync(GetRentalsQuery query, CancellationToken cancellationToken = default)
    {
        var rentals = await rentalRepository.GetAllAsync(cancellationToken);
        return rentals.Select(rental => rental.ToDto()).ToArray();
    }
}
