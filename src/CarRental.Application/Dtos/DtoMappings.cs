using CarRental.Domain.Entities;

namespace CarRental.Application.Dtos;

internal static class DtoMappings
{
    public static CustomerDto ToDto(this Customer customer) =>
        new(customer.Id, customer.FullName, customer.Address, customer.Email);

    public static RentalDto ToDto(this Rental rental) =>
        new(rental.Id, rental.CustomerId, rental.CarId, rental.StartDate, rental.EndDate, rental.Status);

    public static AvailableCarDto ToDto(this Car car) => new(car.Id, car.Type, car.Model);
}
