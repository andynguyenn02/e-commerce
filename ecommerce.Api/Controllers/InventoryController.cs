using ecommerce.Application.Inventory.Commands.Upload;
using ecommerce.Application.Inventory.Queries.GetJobList;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController(ISender sender) : ControllerBase
{
    [Authorize(Policy = "Admin")]
    [HttpGet("jobs")]
    public async Task<IActionResult> GetAllInventoryJob(CancellationToken ct)
    {
        var list = await sender.Send(new GetJobListQuery(), ct);

        return Ok(list);
    }

    [Authorize(Policy = "Admin")]
    [HttpPost("job")]
    public async Task<IActionResult> UploadFile(IFormFile formFile, CancellationToken ct)
    {
        await using var stream = formFile.OpenReadStream();

        await sender.Send(new UploadCommand(stream, formFile.FileName), ct);

        return Created();
    }
}