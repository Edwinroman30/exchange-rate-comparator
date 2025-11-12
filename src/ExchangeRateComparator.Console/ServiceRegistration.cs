using ExchangeRateComparator.Application;
using ExchangeRateComparator.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExchangeRateComparator.Console;

/// <summary>
/// Extension methods for registering Console application services.
/// Orchestrates service registration from all layers.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Registers all application services from all layers.
    /// This is the main entry point for service configuration.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddExchangeRateServices(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Register configuration
        services.AddSingleton(configuration);
        
        // Configure logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        // Register Application layer services (business logic)
        services.AddApplicationServices();
        
        // Register Infrastructure layer services (data access, external APIs)
        // Note: Infrastructure handles its own configuration with IOptions
        services.AddInfrastructureServices(configuration);
        
        return services;
    }
    
    /// <summary>
    /// Builds the configuration from JSON files and environment variables.
    /// </summary>
    /// <returns>The built configuration.</returns>
    public static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
    }
}
