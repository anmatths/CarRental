using CarRental.Application.Abstractions;
using CarRental.Application.Dtos;
using CarRental.Application.Exceptions;

namespace CarRental.Application.Commands.ModifyRental;

public sealed class ModifyRentalCommandHandler(IRentalRepository rentalRepository)
{
    public async Task<RentalDto> HandleAsync(ModifyRentalCommand command, CancellationToken cancellationToken = default)
    {
        var rental = await rentalRepository.GetByIdAsync(command.RentalId, cancellationToken)
            ?? throw new RentalNotFoundException(command.RentalId);

        var activeRentals = await rentalRepository.GetActiveRentalsForCarAsync(rental.CarId, cancellationToken);
        if (activeRentals.Any(existing => existing.Id != rental.Id && existing.Overlaps(command.StartDate, command.EndDate)))
            throw new CarNotAvailableException(rental.CarId);

        rental.ChangePeriod(command.StartDate, command.EndDate);
        await rentalRepository.UpdateAsync(rental, cancellationToken);
        return rental.ToDto();
    }
}
