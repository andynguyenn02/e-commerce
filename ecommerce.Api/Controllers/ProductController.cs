using ecommerce.Application.Products.Commands.CreateProduct;
using ecommerce.Application.Products.Commands.DeleteProduct;
using ecommerce.Application.Products.Commands.UpdatePrice;
using ecommerce.Application.Products.Commands.UpdateProduct;
using ecommerce.Application.Products.Queries.GetAllProducts;
using ecommerce.Application.Products.Queries.GetProductByCategoryId;
using ecommerce.Application.Products.Queries.GetProductById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll(CancellationToken ct)
    {
        return await sender.Send(new GetAllProductsQuery(), ct);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetProductByIdDto>> GetByProductId(Guid id, CancellationToken ct)
    {
        return await sender.Send(new GetProductByIdQuery(id), ct);
    }

    [AllowAnonymous]
    [HttpGet("/category/{categoryId:guid}")]
    public async Task<ActionResult<List<GetProductByCategoryDto>>> GetByCategoryId(Guid categoryId,
        CancellationToken ct)
    {
        return await sender.Send(new GetProductByCategoryQuery(categoryId), ct);
    }

    [Authorize(Policy = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateProductCommand request, CancellationToken ct)
    {
        var id = await sender.Send(request, ct);

        return CreatedAtAction(nameof(GetByProductId), new { id }, id);
    }

    [Authorize(Policy = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateProduct(Guid id, UpdateProductDto dto, CancellationToken ct)
    {
        await sender.Send(new UpdateProductCommand(id, dto), ct);
        return NoContent();
    }

    [Authorize(Policy = "Admin")]
    [HttpPatch("{id:guid}/price")]
    public async Task<ActionResult> UpdatePrice(Guid id, [FromBody] UpdatePriceDto price, CancellationToken ct)
    {
        await sender.Send(new UpdatePriceCommand(id, price), ct);
        return NoContent();
    }

    [Authorize(Policy = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteProduct(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteProductCommand(id), ct);

        return NoContent();
    }
}