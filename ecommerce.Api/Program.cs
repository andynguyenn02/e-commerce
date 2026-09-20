using ecommerce.Api;
using FluentValidation;
using ecommerce.Application;
using ecommerce.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                        // phục vụ /openapi/v1.json
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "ecommerce API v1"));
}
app.UseExceptionHandler();
app.MapControllers();
app.MapGet("/", () => "Hello World!");

app.Run();