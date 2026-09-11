using CarRental.Application.Abstractions;
using CarRental.Application.Dtos;
using CarRental.Application.Exceptions;
using CarRental.Domain.Entities;

namespace CarRental.Application.Commands.RegisterRental;

public sealed class RegisterRentalCommandHandler(
    ICustomerRepository customerRepository,
    ICarRepository carRepository,
    IRentalRepository rentalRepository)
{
    public async Task<RentalDto> HandleAsync(RegisterRentalCommand command, CancellationToken cancellationToken = default)
    {
        if (await customerRepository.GetByIdAsync(command.CustomerId, cancellationToken) is null)
            throw new CustomerNotFoundException(command.CustomerId);

        if (await carRepository.GetByIdAsync(command.CarId, cancellationToken) is null)
            throw new CarNotFoundException(command.CarId);

        var activeRentals = await rentalRepository.GetActiveRentalsForCarAsync(
            command.CarId, command.StartDate, command.EndDate, cancellationToken);
        if (activeRentals.Any(rental => rental.Overlaps(command.StartDate, command.EndDate)))
            throw new CarNotAvailableException(command.CarId);

        var rental = Rental.Create(command.CustomerId, command.CarId, command.StartDate, command.EndDate);
        await rentalRepository.AddAsync(rental, cancellationToken);
        return rental.ToDto();
    }
}
