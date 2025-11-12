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
    Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║       Exchange Rate Comparator - Interactive Demo         ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
    Console.WriteLine();

    // Display supported currencies
    var supportedCurrencies = new[] { "USD", "EUR", "GBP" };
    Console.WriteLine("📊 Supported Currencies:");
    Console.WriteLine("   • USD - United States Dollar");
    Console.WriteLine("   • EUR - Euro");
    Console.WriteLine("   • GBP - British Pound Sterling");
    Console.WriteLine();

    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine();

    // Get source currency
    string sourceCurrency;
    while (true)
    {
        Console.Write("Enter source currency (e.g., USD): ");
        sourceCurrency = Console.ReadLine()?.Trim().ToUpperInvariant() ?? "";
        
        if (supportedCurrencies.Contains(sourceCurrency))
        {
            break;
        }
        
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Invalid currency '{sourceCurrency}'. Please use one of: {string.Join(", ", supportedCurrencies)}");
        Console.ResetColor();
    }

    // Get target currency
    string targetCurrency;
    while (true)
    {
        Console.Write("Enter target currency (e.g., EUR): ");
        targetCurrency = Console.ReadLine()?.Trim().ToUpperInvariant() ?? "";
        
        if (supportedCurrencies.Contains(targetCurrency))
        {
            if (targetCurrency == sourceCurrency)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠️  Source and target currencies are the same. Please choose different currencies.");
                Console.ResetColor();
                continue;
            }
            break;
        }
        
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Invalid currency '{targetCurrency}'. Please use one of: {string.Join(", ", supportedCurrencies)}");
        Console.ResetColor();
    }

    // Get amount
    decimal amount;
    while (true)
    {
        Console.Write($"Enter amount in {sourceCurrency} (e.g., 1000.00): ");
        var amountInput = Console.ReadLine()?.Trim() ?? "";
        
        if (decimal.TryParse(amountInput, out amount) && amount > 0)
        {
            break;
        }
        
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("❌ Invalid amount. Please enter a positive number.");
        Console.ResetColor();
    }

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
    
    Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                        RESULTS                             ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"Best Rate Found:    {result.BestRate:F4}");
    Console.WriteLine($"Converted Amount:   {result.ConvertedAmount:N2} {request.TargetCurrency}");
    Console.ResetColor();
    Console.WriteLine($"Provider:           {result.Provider}");
    Console.WriteLine($"Execution Time:     {result.ExecutionTimeMs} ms");
    Console.WriteLine();
    
    // Calculate savings comparison
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
