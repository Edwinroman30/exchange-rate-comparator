using ExchangeRateComparator.Application.Interfaces;
using ExchangeRateComparator.Console;
using ExchangeRateComparator.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

// Build configuration
var configuration = ServiceRegistration.BuildConfiguration();

// Configure services using extension method
var services = new ServiceCollection();
services.AddExchangeRateServices(configuration);

var serviceProvider = services.BuildServiceProvider();

// Execute exchange rate comparison
try
{
    var supportedCurrencies = new[] { "USD", "EUR", "GBP" };
    
    DisplayWelcomeHeader();
    DisplaySupportedCurrencies(supportedCurrencies);
    
    var sourceCurrency = GetSourceCurrency(supportedCurrencies);
    var targetCurrency = GetTargetCurrency(supportedCurrencies, sourceCurrency);
    var amount = GetAmount(sourceCurrency);
    
    await ExecuteExchangeRateComparison(serviceProvider, sourceCurrency, targetCurrency, amount);
    
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
catch (InvalidOperationException ex)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                         ERROR                              ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine($"(X) {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("Tip: Check your appsettings.json configuration");
    Console.WriteLine("Make sure at least one API is enabled and configured.");
    Console.ResetColor();
    Environment.Exit(1);
}
catch (OperationCanceledException)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Operation timed out or was cancelled.");
    Console.WriteLine();
    Console.WriteLine("Possible reasons:");
    Console.WriteLine("• API endpoints are not responding");
    Console.WriteLine("• Network connectivity issues");
    Console.WriteLine("• Timeout is too short (increase in appsettings.json)");
    Console.ResetColor();
    Environment.Exit(130);
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                    UNEXPECTED ERROR                        ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine($"(X) {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("Stack Trace:");
    Console.WriteLine(ex.StackTrace);
    Console.ResetColor();
    Environment.Exit(1);
}

// ============================================================================
// Helper Methods for User Interaction
// ============================================================================

/// <summary>
/// Displays the welcome header for the application.
/// </summary>
static void DisplayWelcomeHeader()
{
    Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║       Exchange Rate Comparator - Interactive Demo         ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
}

/// <summary>
/// Displays the list of supported currencies.
/// </summary>
/// <param name="supportedCurrencies">Array of supported currency codes.</param>
static void DisplaySupportedCurrencies(string[] supportedCurrencies)
{
    Console.WriteLine("📊 Supported Currencies:");
    Console.WriteLine("   • USD - United States Dollar");
    Console.WriteLine("   • EUR - Euro");
    Console.WriteLine("   • GBP - British Pound Sterling");
    Console.WriteLine();
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine();
}

/// <summary>
/// Prompts the user to enter a source currency and validates the input.
/// </summary>
/// <param name="supportedCurrencies">Array of supported currency codes.</param>
/// <returns>The validated source currency code.</returns>
static string GetSourceCurrency(string[] supportedCurrencies)
{
    while (true)
    {
        Console.Write("Enter source currency (e.g., USD): ");
        var sourceCurrency = Console.ReadLine()?.Trim().ToUpperInvariant() ?? "";
        
        if (supportedCurrencies.Contains(sourceCurrency))
        {
            return sourceCurrency;
        }
        
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Invalid currency '{sourceCurrency}'. Please use one of: {string.Join(", ", supportedCurrencies)}");
        Console.ResetColor();
    }
}

/// <summary>
/// Prompts the user to enter a target currency and validates the input.
/// </summary>
/// <param name="supportedCurrencies">Array of supported currency codes.</param>
/// <param name="sourceCurrency">The source currency to compare against.</param>
/// <returns>The validated target currency code.</returns>
static string GetTargetCurrency(string[] supportedCurrencies, string sourceCurrency)
{
    while (true)
    {
        Console.Write("Enter target currency (e.g., EUR): ");
        var targetCurrency = Console.ReadLine()?.Trim().ToUpperInvariant() ?? "";
        
        if (!supportedCurrencies.Contains(targetCurrency))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Invalid currency '{targetCurrency}'. Please use one of: {string.Join(", ", supportedCurrencies)}");
            Console.ResetColor();
            continue;
        }
        
        if (targetCurrency == sourceCurrency)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("⚠️  Source and target currencies are the same. Please choose different currencies.");
            Console.ResetColor();
            continue;
        }
        
        return targetCurrency;
    }
}

/// <summary>
/// Prompts the user to enter an amount and validates the input.
/// </summary>
/// <param name="sourceCurrency">The source currency for display purposes.</param>
/// <returns>The validated amount.</returns>
static decimal GetAmount(string sourceCurrency)
{
    while (true)
    {
        Console.Write($"Enter amount in {sourceCurrency} (e.g., 1000.00): ");
        var amountInput = Console.ReadLine()?.Trim() ?? "";
        
        if (decimal.TryParse(amountInput, out var amount) && amount > 0)
        {
            return amount;
        }
        
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("❌ Invalid amount. Please enter a positive number.");
        Console.ResetColor();
    }
}

/// <summary>
/// Executes the exchange rate comparison and displays the results.
/// </summary>
/// <param name="serviceProvider">The service provider for dependency injection.</param>
/// <param name="sourceCurrency">The source currency code.</param>
/// <param name="targetCurrency">The target currency code.</param>
/// <param name="amount">The amount to convert.</param>
static async Task ExecuteExchangeRateComparison(
    ServiceProvider serviceProvider,
    string sourceCurrency,
    string targetCurrency,
    decimal amount)
{
    Console.WriteLine();
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine();

    var comparator = serviceProvider.GetRequiredService<IExchangeRateComparatorService>();
    var request = new ExchangeRequest(sourceCurrency, targetCurrency, amount);
    
    Console.WriteLine($"🔍 Querying multiple exchange rate APIs in parallel...");
    Console.WriteLine($"   Converting {request.Amount:N2} {request.SourceCurrency} → {request.TargetCurrency}");
    Console.WriteLine();

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    var result = await comparator.GetBestExchangeRateAsync(request, cts.Token);
    
    DisplayResults(result, request.TargetCurrency);
    DisplaySavingsInformation(amount, sourceCurrency);
}

/// <summary>
/// Displays the exchange rate comparison results.
/// </summary>
/// <param name="result">The comparison result.</param>
/// <param name="targetCurrency">The target currency code.</param>
static void DisplayResults(dynamic result, string targetCurrency)
{
    Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                        RESULTS                             ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Best Rate Found:    {result.BestRate:F4}");
    Console.WriteLine($"Converted Amount:   {result.ConvertedAmount:N2} {targetCurrency}");
    Console.ResetColor();
    Console.WriteLine($"Provider:           {result.Provider}");
    Console.WriteLine($"Execution Time:     {result.ExecutionTimeMs} ms");
    Console.WriteLine();
}

/// <summary>
/// Displays information about potential savings from using a rate comparator.
/// </summary>
/// <param name="amount">The original amount.</param>
/// <param name="sourceCurrency">The source currency code.</param>
static void DisplaySavingsInformation(decimal amount, string sourceCurrency)
{
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("Why use a rate comparator?");
    Console.WriteLine($"By comparing multiple providers, you got the BEST rate!");
    Console.WriteLine($"Even a 1% better rate on {amount:N2} {sourceCurrency} saves you {(amount * 0.01m):N2} {sourceCurrency}");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine();
}
