using CarRental.Api.Contracts.Customers;
using CarRental.Application.Commands.RegisterCustomer;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController(RegisterCustomerCommandHandler registerCustomerHandler) : ControllerBase
{
    /// <summary>Registers a new customer.</summary>
    [HttpPost]
    [ProducesResponseType<CustomerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CustomerResponse>> Register(
        RegisterCustomerRequest request,
        CancellationToken cancellationToken)
    {
        var customer = await registerCustomerHandler.HandleAsync(
            new RegisterCustomerCommand(request.FullName, request.Address, request.Email),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, CustomerResponse.From(customer));
    }
}
