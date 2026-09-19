using System.Reflection;
using ecommerce.Application.Common.Behaviors;
using FluentValidation;

namespace ecommerce.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR((cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        }));
        
        services.AddValidatorsFromAssembly(assembly);
        ;
        
        return services;
    }
}