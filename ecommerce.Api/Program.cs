using System.Text.Json.Serialization;
using ecommerce.Api;
using ecommerce.Api.Auth;
using ecommerce.Application;
using ecommerce.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter()
    );
});
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApiAuth();

// Allowed origins come from config (Cors:AllowedOrigins) instead of a literal,
// so prod/staging domains don't need an API rebuild to be let in.
// AllowCredentials() is incompatible with AllowAnyOrigin()/"*" (cookie-based
// auth needs an explicit origin list) -- a missing/empty config section just
// means no origin is allowed; never fall back to a wildcard here.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous(); // phục vụ /openapi/v1.json
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "ecommerce API v1"));
}

app.UseExceptionHandler();
app.UseCors("AllowedOrigins"); // must be before Authentication/Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();