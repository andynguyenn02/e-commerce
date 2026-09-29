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

// CORS for Vite dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("ViteDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
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
app.UseCors("ViteDev"); // must be before Authentication/Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();