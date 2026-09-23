using ecommerce.Application.Checkout.Commands.CheckoutCommand;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CheckoutController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutCommand command, CancellationToken ct)
    {
        await sender.Send(command, ct);

        return Ok();
    }
}