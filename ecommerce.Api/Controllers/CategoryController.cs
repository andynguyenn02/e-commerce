using ecommerce.Application.Categories.Commands.CreateCategory;
using ecommerce.Application.Categories.Commands.DeleteCategory;
using ecommerce.Application.Categories.Commands.UpdateCategory;
using ecommerce.Application.Categories.Query;
using ecommerce.Application.Categories.Query.GetCategoryById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoryController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var list = await sender.Send(new GetAllCategoryQuery(), ct);

        return Ok(list);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var item = await sender.Send(new GetCategoryByIdQuery(id), ct);

        return Ok(item);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateCategory(CreateCategoryCommand command, CancellationToken ct)
    {
        var item = await sender.Send(command, ct);

        return CreatedAtAction(nameof(GetById), new { id = item }, item);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, UpdateCategoryDto dto, CancellationToken ct)
    {
        var item = await sender.Send(new UpdateCategoryCommand(id, dto), ct);

        return CreatedAtAction(nameof(GetById), new { id = item }, item);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id, CancellationToken ct)
    {
        await sender.Send(new DeleteCategoryCommand(id), ct);

        return NoContent();
    }
}