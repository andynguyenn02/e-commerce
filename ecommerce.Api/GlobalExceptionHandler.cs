using ecommerce.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Api;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var (statusCode, title) = exception switch
        {
            // business errors (specific types before the BusinessException base)
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            BusinessException => (StatusCodes.Status400BadRequest, "Business rule violated"),

            // 400
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            BadHttpRequestException e => (e.StatusCode, "Bad request"), // body/JSON sai format

            // 409
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Data was modified by another user"),

            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        // only our own exceptions carry messages that are safe to show the client
        var message = exception is BusinessException or ValidationException ? exception.Message : title;
        var code = (exception as BusinessException)?.Code;

        if (statusCode >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { error = message, code }, ct);
        return true;
    }
}