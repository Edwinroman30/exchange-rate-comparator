using ExchangeRateComparator.Application.Interfaces;
using ExchangeRateComparator.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ExchangeRateComparator.Application;

/// <summary>
/// Extension methods for registering Application layer services.
/// Handles business logic and use case services.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Registers all application services including the exchange rate comparator.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register the main comparator service
        services.AddTransient<IExchangeRateComparatorService, ExchangeRateComparatorService>();
        
        return services;
    }
}
