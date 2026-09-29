using ecommerce.Application.Checkout.Commands.CheckoutCommand;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.Api.Controllers;

[Authorize(Policy = "Customer")]
[ApiController]
[Route("api/[controller]")]
public class CheckoutController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Checkout([FromBody] CheckoutCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        return Ok(result);
    }
}