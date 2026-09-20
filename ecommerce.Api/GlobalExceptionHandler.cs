using Microsoft.AspNetCore.Diagnostics;

namespace ecommerce.Api;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            FluentValidation.ValidationException => StatusCodes.Status400BadRequest,
            KeyNotFoundException                 => StatusCodes.Status404NotFound,
            _                                    => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { error = exception.Message }, ct);
        return true;   
    }
}