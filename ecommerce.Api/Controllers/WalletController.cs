using ecommerce.Application.Wallets.Queries.GetCurrentBalance;
using ecommerce.Application.Wallets.Queries.GetWalletTransactions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController(ISender sender) : ControllerBase
{
    [HttpGet("balance")]
    public async Task<IActionResult> GetCurrentBalance(CancellationToken cancellationToken)
    {
        var balance = await sender.Send(new GetCurrentBalanceQuery(), cancellationToken);

        return Ok(balance);
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(CancellationToken cancellationToken)
    {
        var transactions = await sender.Send(new GetWalletTransactionsQuery(), cancellationToken);

        return Ok(transactions);
    }
}