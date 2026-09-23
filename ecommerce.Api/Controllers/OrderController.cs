using ecommerce.Application.Orders.Queries.GetOrderDetail;
using ecommerce.Application.Orders.Queries.GetOrderSummary;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetOrderSummary(CancellationToken ct)
    {
        var lists = await sender.Send(new GetOrderSummaryQuery(), ct);

        return Ok(lists);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderDetail(Guid id, CancellationToken ct)
    {
        var detail = await sender.Send(new GetOrderDetailQuery(id), ct);

        return Ok(detail);
    }
}