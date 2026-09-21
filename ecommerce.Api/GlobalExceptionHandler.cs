using System.Text.Json;
using ecommerce.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Api;

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var status = exception switch
        {
            // 400
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            BadRequestException => (StatusCodes.Status400BadRequest, "Bad request"),
            BadHttpRequestException e => (e.StatusCode, "Bad request"), // body/JSON sai format
            JsonException => (StatusCodes.Status400BadRequest, "Invalid JSON"),
            FormatException => (StatusCodes.Status400BadRequest, "Invalid format"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid argument"),

            // 401 / 403
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),

            // 404
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),

            // 409
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Data was modified by another user"),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Data already exists"),

            // 5xx
            NotImplementedException => (StatusCodes.Status501NotImplemented, "Not implemented"),
            TimeoutException => (StatusCodes.Status504GatewayTimeout, "Request timed out"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        context.Response.StatusCode = status.Item1;
        await context.Response.WriteAsJsonAsync(new { error = exception.Message }, ct);
        return true;
    }
}