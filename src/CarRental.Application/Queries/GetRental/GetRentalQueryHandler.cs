using CarRental.Application.Abstractions;
using CarRental.Application.Dtos;
using CarRental.Application.Exceptions;

namespace CarRental.Application.Queries.GetRental;

public sealed class GetRentalQueryHandler(IRentalRepository rentalRepository)
{
    public async Task<RentalDto> HandleAsync(GetRentalQuery query, CancellationToken cancellationToken = default)
    {
        var rental = await rentalRepository.GetByIdAsync(query.RentalId, cancellationToken)
            ?? throw new RentalNotFoundException(query.RentalId);

        return rental.ToDto();
    }
}
