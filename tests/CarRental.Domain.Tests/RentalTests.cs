using CarRental.Domain.Entities;
using CarRental.Domain.Exceptions;

namespace CarRental.Domain.Tests;

public sealed class RentalTests
{
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid CarId = Guid.NewGuid();
    private static readonly DateOnly StartDate = new(2026, 10, 1);
    private static readonly DateOnly EndDate = new(2026, 10, 5);

    [Fact]
    public void Create_WithValidData_CreatesActiveRental()
    {
        var rental = CreateRental();

        Assert.NotEqual(Guid.Empty, rental.Id);
        Assert.Equal(CustomerId, rental.CustomerId);
        Assert.Equal(CarId, rental.CarId);
        Assert.Equal(RentalStatus.Active, rental.Status);
    }

    [Theory]
    [InlineData(2026, 10, 1)]
    [InlineData(2026, 9, 30)]
    public void Create_WithEndDateNotAfterStartDate_ThrowsInvalidRentalPeriodException(int year, int month, int day)
    {
        var exception = Assert.Throws<InvalidRentalPeriodException>(
            () => Rental.Create(CustomerId, CarId, StartDate, new DateOnly(year, month, day)));

        Assert.Contains("end date", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_WithEmptyCustomerId_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => Rental.Create(Guid.Empty, CarId, StartDate, EndDate));
    }

    [Fact]
    public void Create_WithEmptyCarId_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => Rental.Create(CustomerId, Guid.Empty, StartDate, EndDate));
    }

    [Theory]
    [InlineData(2026, 10, 2, 2026, 10, 4)]
    [InlineData(2026, 9, 30, 2026, 10, 2)]
    [InlineData(2026, 10, 4, 2026, 10, 7)]
    [InlineData(2026, 9, 30, 2026, 10, 7)]
    public void Overlaps_WithIntersectingPeriods_ReturnsTrue(
        int startYear, int startMonth, int startDay,
        int endYear, int endMonth, int endDay)
    {
        var rental = CreateRental();

        var overlaps = rental.Overlaps(
            new DateOnly(startYear, startMonth, startDay),
            new DateOnly(endYear, endMonth, endDay));

        Assert.True(overlaps);
    }

    [Theory]
    [InlineData(2026, 10, 5, 2026, 10, 10)]
    [InlineData(2026, 9, 25, 2026, 10, 1)]
    public void Overlaps_WithAdjacentPeriods_ReturnsFalse(
        int startYear, int startMonth, int startDay,
        int endYear, int endMonth, int endDay)
    {
        var rental = CreateRental();

        var overlaps = rental.Overlaps(
            new DateOnly(startYear, startMonth, startDay),
            new DateOnly(endYear, endMonth, endDay));

        Assert.False(overlaps);
    }

    [Fact]
    public void Cancel_SetsStatusToCancelled()
    {
        var rental = CreateRental();

        rental.Cancel();

        Assert.Equal(RentalStatus.Cancelled, rental.Status);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_IsIdempotent()
    {
        var rental = CreateRental();
        rental.Cancel();

        rental.Cancel();

        Assert.Equal(RentalStatus.Cancelled, rental.Status);
    }

    [Fact]
    public void Overlaps_WhenRentalIsCancelled_ReturnsFalse()
    {
        var rental = CreateRental();
        rental.Cancel();

        Assert.False(rental.Overlaps(new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 4)));
    }

    [Fact]
    public void ChangePeriod_WhenRentalIsActive_ChangesPeriod()
    {
        var rental = CreateRental();
        var newStartDate = new DateOnly(2026, 10, 10);
        var newEndDate = new DateOnly(2026, 10, 15);

        rental.ChangePeriod(newStartDate, newEndDate);

        Assert.Equal(newStartDate, rental.StartDate);
        Assert.Equal(newEndDate, rental.EndDate);
    }

    [Fact]
    public void ChangePeriod_WithInvalidDates_ThrowsInvalidRentalPeriodException()
    {
        var rental = CreateRental();

        Assert.Throws<InvalidRentalPeriodException>(() => rental.ChangePeriod(EndDate, EndDate));
    }

    [Fact]
    public void ChangePeriod_WhenRentalIsCancelled_ThrowsInvalidRentalOperationException()
    {
        var rental = CreateRental();
        rental.Cancel();

        Assert.Throws<InvalidRentalOperationException>(
            () => rental.ChangePeriod(new DateOnly(2026, 10, 6), new DateOnly(2026, 10, 10)));
    }

    private static Rental CreateRental() => Rental.Create(CustomerId, CarId, StartDate, EndDate);
}
