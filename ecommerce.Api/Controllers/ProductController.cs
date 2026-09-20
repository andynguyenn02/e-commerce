using System;
using ecommerce.Application.Products.Commands.CreateProduct;
using ecommerce.Application.Products.Commands.DeleteProduct;
using ecommerce.Application.Products.Commands.UpdateProduct;
using ecommerce.Application.Products.Queries.GetAllProducts;
using ecommerce.Application.Products.Queries.GetProductByCategoryId;
using ecommerce.Application.Products.Queries.GetProductById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetAll(CancellationToken ct)
    {
        return await sender.Send(new GetAllProductsQuery(), ct);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetProductByIdDto>> GetByProductId(Guid id, CancellationToken ct)
    {
        return await sender.Send(new GetProductByIdQuery(id), ct);
    }

    [HttpGet("/category/{categoryId:guid}")]
    public async Task<ActionResult<List<GetProductByCategoryDto>>> GetByCategoryId(Guid categoryId,
        CancellationToken ct)
    {
        return await sender.Send(new GetProductByCategoryQuery(categoryId), ct);
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateProductCommand request, CancellationToken ct)
    {
       var id = await sender.Send(request, ct);
       
       return CreatedAtAction(nameof(GetByProductId), new { id }, id);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateProduct(Guid id, UpdateProductDto dto, CancellationToken ct)
    {
        await sender.Send(new UpdateProductCommand(id, dto), ct);     
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteProduct(DeleteProductCommand command, CancellationToken ct)
    {
        await sender.Send(command, ct);
        
        return NoContent();
    }
}
