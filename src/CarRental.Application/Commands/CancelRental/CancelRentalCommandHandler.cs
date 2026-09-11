using CarRental.Application.Abstractions;
using CarRental.Application.Dtos;
using CarRental.Application.Exceptions;

namespace CarRental.Application.Commands.CancelRental;

public sealed class CancelRentalCommandHandler(IRentalRepository rentalRepository)
{
    public async Task<RentalDto> HandleAsync(CancelRentalCommand command, CancellationToken cancellationToken = default)
    {
        var rental = await rentalRepository.GetByIdAsync(command.RentalId, cancellationToken)
            ?? throw new RentalNotFoundException(command.RentalId);

        rental.Cancel();
        await rentalRepository.UpdateAsync(rental, cancellationToken);
        return rental.ToDto();
    }
}
