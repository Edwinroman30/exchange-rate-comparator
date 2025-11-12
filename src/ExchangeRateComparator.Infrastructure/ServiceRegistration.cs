using ExchangeRateComparator.Domain.Interfaces;
using ExchangeRateComparator.Infrastructure.Adapters;
using ExchangeRateComparator.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ExchangeRateComparator.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services.
/// Handles API adapters, HTTP client configuration, and infrastructure settings.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Registers all infrastructure services including configuration, HTTP clients, and API adapters.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register configuration with IOptions pattern
        services.Configure<ExchangeRateApiSettings>(
            configuration.GetSection(ExchangeRateApiSettings.SectionName));

        // Register HTTP clients for each API adapter
        services.AddApiHttpClients();
        
        // Register exchange rate providers
        services.AddExchangeRateProviders();

        return services;
    }

    /// <summary>
    /// Registers HTTP clients for all API adapters with configuration-based settings using IOptions.
    /// </summary>
    private static IServiceCollection AddApiHttpClients(this IServiceCollection services)
    {
        // API 1 - JSON Adapter
        services.AddHttpClient<Api1JsonAdapter>((serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ExchangeRateApiSettings>>().Value;
            if (settings.Api1.Enabled)
            {
                client.BaseAddress = new Uri(settings.Api1.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(settings.Api1.TimeoutSeconds);
            }
        });

        // API 2 - XML Adapter
        services.AddHttpClient<Api2XmlAdapter>((serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ExchangeRateApiSettings>>().Value;
            if (settings.Api2.Enabled)
            {
                client.BaseAddress = new Uri(settings.Api2.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(settings.Api2.TimeoutSeconds);
            }
        });

        // API 3 - Complex JSON Adapter
        services.AddHttpClient<Api3JsonAdapter>((serviceProvider, client) =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ExchangeRateApiSettings>>().Value;
            if (settings.Api3.Enabled)
            {
                client.BaseAddress = new Uri(settings.Api3.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(settings.Api3.TimeoutSeconds);
            }
        });

        return services;
    }

    /// <summary>
    /// Registers exchange rate providers conditionally based on configuration.
    /// Only enabled providers are registered in the DI container.
    /// </summary>
    private static IServiceCollection AddExchangeRateProviders(this IServiceCollection services)
    {
        // Register API 1 Provider (conditionally)
        services.AddTransient<IExchangeRateProvider>(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ExchangeRateApiSettings>>().Value;
            if (!settings.Api1.Enabled)
            {
                throw new InvalidOperationException("Api1 is disabled in configuration.");
            }
            return serviceProvider.GetRequiredService<Api1JsonAdapter>();
        });

        // Register API 2 Provider (conditionally)
        services.AddTransient<IExchangeRateProvider>(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ExchangeRateApiSettings>>().Value;
            if (!settings.Api2.Enabled)
            {
                throw new InvalidOperationException("Api2 is disabled in configuration.");
            }
            return serviceProvider.GetRequiredService<Api2XmlAdapter>();
        });

        // Register API 3 Provider (conditionally)
        services.AddTransient<IExchangeRateProvider>(serviceProvider =>
        {
            var settings = serviceProvider.GetRequiredService<IOptions<ExchangeRateApiSettings>>().Value;
            if (!settings.Api3.Enabled)
            {
                throw new InvalidOperationException("Api3 is disabled in configuration.");
            }
            return serviceProvider.GetRequiredService<Api3JsonAdapter>();
        });

        return services;
    }
}
