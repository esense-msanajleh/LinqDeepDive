using LinqDeepDive.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LinqDeepDive.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LinqDemoService>();
        return services;
    }
}
