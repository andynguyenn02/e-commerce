using ecommerce.Application.Carts.Commands.AddItemToCart;
using ecommerce.Application.Carts.Commands.ClearCart;
using ecommerce.Application.Carts.Commands.RemoveCartItem;
using ecommerce.Application.Carts.Commands.UpdateCartItemQuantity;
using ecommerce.Application.Carts.Queries.GetMyCart;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController(ISender sender) : ControllerBase
{
    [HttpGet("mycart")]
    public async Task<IActionResult> GetMyCart(CancellationToken ct = default)
    {
        var cart = await sender.Send(new GetMyCartCommand(), ct);

        return Ok(cart);
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddItemToCart([FromBody] AddItemToCartCommand command,
        CancellationToken ct = default)
    {
        await sender.Send(command, ct);

        return Created();
    }

    [HttpDelete("remove/{cartItemId:guid}")]
    public async Task<IActionResult> RemoveItemFromCart(Guid cartItemId, CancellationToken ct = default)
    {
        await sender.Send(new RemoveCartItemCommand(cartItemId), ct);

        return NoContent();
    }

    [HttpPatch("update/{cartItemId:guid}")]
    public async Task<IActionResult> UpdateQuantityCartItem(Guid cartItemId,
        [FromBody] UpdateCartItemDto command,
        CancellationToken ct = default)
    {
        await sender.Send(new UpdateCartItemQuantityCommand(cartItemId, command), ct);

        return NoContent();
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart(CancellationToken ct = default)
    {
        await sender.Send(new ClearCartCommand(), ct);

        return NoContent();
    }
}