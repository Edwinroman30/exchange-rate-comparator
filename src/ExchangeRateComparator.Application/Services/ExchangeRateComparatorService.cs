using System.Diagnostics;
using ExchangeRateComparator.Application.Interfaces;
using ExchangeRateComparator.Domain.Entities;
using ExchangeRateComparator.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ExchangeRateComparator.Application.Services;

/// <summary>
/// Service that queries multiple exchange rate providers in parallel and returns the best rate
/// </summary>
public class ExchangeRateComparatorService : IExchangeRateComparatorService
{
    private readonly IEnumerable<IExchangeRateProvider> _providers;
    private readonly ILogger<ExchangeRateComparatorService> _logger;

    public ExchangeRateComparatorService(
        IEnumerable<IExchangeRateProvider> providers,
        ILogger<ExchangeRateComparatorService> logger)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(logger);

        _providers = providers;
        _logger = logger;
    }

    /// <summary>
    /// Queries all exchange rate providers in parallel and returns the best conversion rate
    /// </summary>
    /// <param name="request">The exchange request containing currencies and amount</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The best exchange rate result with provider information</returns>
    public async Task<ExchangeResponse> GetBestExchangeRateAsync(ExchangeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.SourceCurrency))
            throw new ArgumentException("Source currency cannot be empty", nameof(request));

        if (string.IsNullOrWhiteSpace(request.TargetCurrency))
            throw new ArgumentException("Target currency cannot be empty", nameof(request));

        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(request));

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Starting exchange rate comparison for {Amount} {SourceCurrency} to {TargetCurrency}",
            request.Amount, request.SourceCurrency, request.TargetCurrency);

        // Query all providers in parallel
        var tasks = _providers.Select(provider => 
            GetExchangeRateWithErrorHandlingAsync(provider, request, cancellationToken));
        
        var results = await Task.WhenAll(tasks);

        stopwatch.Stop();

        // Filter successful results and select the highest converted amount
        var successfulResults = results.Where(r => r.IsSuccess).ToList();

        if (successfulResults.Count == 0)
        {
            _logger.LogError("All providers failed to return exchange rates");
            throw new InvalidOperationException("All exchange rate providers failed. Please try again later.");
        }

        // Select the best result (highest converted amount)
        var bestResult = successfulResults
            .OrderByDescending(r => r.ConvertedAmount)
            .First();

        _logger.LogInformation(
            "Best rate found: {Rate} from {Provider} with converted amount {ConvertedAmount}",
            bestResult.Rate, bestResult.ProviderName, bestResult.ConvertedAmount);

        return new ExchangeResponse
        {
            BestRate = bestResult.Rate,
            ConvertedAmount = bestResult.ConvertedAmount,
            Provider = bestResult.ProviderName,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds
        };
    }

    /// <summary>
    /// Wraps provider call with error handling to ensure one failed provider doesn't break the entire operation
    /// </summary>
    private async Task<ExchangeResult> GetExchangeRateWithErrorHandlingAsync(
        IExchangeRateProvider provider,
        ExchangeRequest request,
        CancellationToken cancellationToken)
    {
        var providerStopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Querying provider {ProviderName}", provider.ProviderName);

            var result = await provider.GetExchangeRateAsync(request, cancellationToken);

            providerStopwatch.Stop();

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Provider {ProviderName} returned rate {Rate} with converted amount {ConvertedAmount} in {ExecutionTimeMs}ms",
                    provider.ProviderName, result.Rate, result.ConvertedAmount, providerStopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning(
                    "Provider {ProviderName} failed: {ErrorMessage}",
                    provider.ProviderName, result.ErrorMessage);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            providerStopwatch.Stop();
            _logger.LogWarning(
                "Provider {ProviderName} operation was cancelled or timed out after {ExecutionTimeMs}ms",
                provider.ProviderName, providerStopwatch.ElapsedMilliseconds);

            return ExchangeResult.Failure(
                provider.ProviderName,
                "Operation timed out",
                providerStopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            providerStopwatch.Stop();
            _logger.LogError(
                ex,
                "Provider {ProviderName} threw an exception after {ExecutionTimeMs}ms",
                provider.ProviderName, providerStopwatch.ElapsedMilliseconds);

            return ExchangeResult.Failure(
                provider.ProviderName,
                ex.Message,
                providerStopwatch.ElapsedMilliseconds);
        }
    }
}
