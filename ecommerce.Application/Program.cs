using ecommerce.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();