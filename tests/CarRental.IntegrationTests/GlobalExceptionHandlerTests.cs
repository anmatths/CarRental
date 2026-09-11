using CarRental.Api.ExceptionHandling;
using CarRental.Application.Exceptions;
using CarRental.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace CarRental.IntegrationTests;

public sealed class GlobalExceptionHandlerTests
{
    [Theory]
    [MemberData(nameof(KnownExceptions))]
    public async Task TryHandleAsync_MapsBusinessExceptionsToTheExpectedProblemDetails(
        Exception exception,
        int expectedStatus)
    {
        var writer = new CapturingProblemDetailsService();
        var handler = new GlobalExceptionHandler(writer, NullLogger<GlobalExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/rentals";

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.NotNull(writer.ProblemDetails);
        Assert.Equal(expectedStatus, writer.ProblemDetails.Status);
        Assert.Equal("/api/rentals", writer.ProblemDetails.Instance);
        Assert.Equal(exception.Message, writer.ProblemDetails.Detail);
    }

    [Fact]
    public async Task TryHandleAsync_HidesUnexpectedExceptionDetails()
    {
        var writer = new CapturingProblemDetailsService();
        var handler = new GlobalExceptionHandler(writer, NullLogger<GlobalExceptionHandler>.Instance);
        var exception = new InvalidOperationException("Host=postgres;Password=secret;StackTrace=/internal/path");

        await handler.TryHandleAsync(new DefaultHttpContext(), exception, CancellationToken.None);

        Assert.NotNull(writer.ProblemDetails);
        Assert.Equal(StatusCodes.Status500InternalServerError, writer.ProblemDetails.Status);
        Assert.Equal("An unexpected error occurred", writer.ProblemDetails.Title);
        Assert.DoesNotContain("secret", writer.ProblemDetails.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("postgres", writer.ProblemDetails.Detail, StringComparison.OrdinalIgnoreCase);
    }

    public static IEnumerable<object[]> KnownExceptions()
    {
        yield return [new CustomerNotFoundException(Guid.NewGuid()), StatusCodes.Status404NotFound];
        yield return [new CarNotFoundException(Guid.NewGuid()), StatusCodes.Status404NotFound];
        yield return [new RentalNotFoundException(Guid.NewGuid()), StatusCodes.Status404NotFound];
        yield return [new CarNotAvailableException(Guid.NewGuid()), StatusCodes.Status409Conflict];
        yield return [new InvalidRentalPeriodException("Invalid period"), StatusCodes.Status400BadRequest];
        yield return [new InvalidRentalOperationException("Invalid operation"), StatusCodes.Status400BadRequest];
        yield return [new DomainValidationException("Invalid domain value"), StatusCodes.Status400BadRequest];
    }

    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetails? ProblemDetails { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            ProblemDetails = context.ProblemDetails;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            ProblemDetails = context.ProblemDetails;
            return ValueTask.FromResult(true);
        }
    }
}
