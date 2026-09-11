using CarRental.Application.Exceptions;
using CarRental.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Api.ExceptionHandling;

/// <summary>Converts expected business exceptions into RFC 9457 problem details responses.</summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            CustomerNotFoundException or CarNotFoundException or RentalNotFoundException =>
                (StatusCodes.Status404NotFound, "Resource not found", exception.Message),
            CarNotAvailableException =>
                (StatusCodes.Status409Conflict, "Car is not available", exception.Message),
            DomainValidationException =>
                (StatusCodes.Status400BadRequest, "Invalid rental request", exception.Message),
            _ => Unexpected(exception)
        };

        var problemDetails = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{status}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private (int Status, string Title, string Detail) Unexpected(Exception exception)
    {
        logger.LogError(exception, "An unhandled exception occurred while processing the request.");
        return (
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred",
            "An unexpected error occurred. Please try again later.");
    }
}
