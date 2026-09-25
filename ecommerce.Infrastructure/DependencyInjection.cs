using ecommerce.Application.Common.Interfaces;
using ecommerce.Infrastructure.BackgroundJobs;
using ecommerce.Infrastructure.Jobs;
using ecommerce.Infrastructure.Persistence;
using ecommerce.Infrastructure.Security;
using ecommerce.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ecommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IInventoryJobQueue, InventoryJobQueue>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.Section));
        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.Section));
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddHostedService<InventoryJobWorker>();

        //file reader
        services.AddSingleton<IInventoryFileReader, CsvInventoryFileReader>();
        services.AddSingleton<IInventoryFileReader, ExcelInventoryFileReader>();

        return services;
    }
}