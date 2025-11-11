using System.Diagnostics;
using System.Text.Json;
using ExchangeRateComparator.Domain.Entities;
using ExchangeRateComparator.Domain.Interfaces;
using ExchangeRateComparator.Infrastructure.Models;
using Microsoft.Extensions.Logging;

namespace ExchangeRateComparator.Infrastructure.Adapters;

/// <summary>
/// Adapter for API1 which returns an exchange RATE (not the converted amount).
/// 
/// INTERPRETATION NOTE: The requirements are ambiguous about what each API returns.
/// Based on the field name "rate" and the requirement to "select the highest conversion amount"
/// (implying different APIs return different values), I interpret API1 as returning
/// an exchange rate (e.g., 0.85) that must be multiplied by the input amount.
/// 
/// Example:
/// - Input: 1000 USD to EUR
/// - API1 returns: { "rate": 0.85 }
/// - Calculation: 1000 * 0.85 = 850 EUR
/// </summary>
public class Api1JsonAdapter : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Api1JsonAdapter> _logger;

    public string ProviderName => "API1-JSON";

    public Api1JsonAdapter(HttpClient httpClient, ILogger<Api1JsonAdapter> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ExchangeResult> GetExchangeRateAsync(ExchangeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var apiRequest = new Api1Request
            {
                From = request.SourceCurrency,
                To = request.TargetCurrency,
                Value = request.Amount
            };

            var json = JsonSerializer.Serialize(apiRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending request to {Provider}", ProviderName);

            var response = await _httpClient.PostAsync("/api/exchange", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var statusCode = (int)response.StatusCode;
                _logger.LogWarning("{Provider} returned status code {StatusCode}", ProviderName, statusCode);
                
                stopwatch.Stop();
                return ExchangeResult.Failure(
                    ProviderName,
                    $"API returned status code {statusCode}",
                    stopwatch.ElapsedMilliseconds);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<Api1Response>(responseJson);

            if (apiResponse == null)
            {
                stopwatch.Stop();
                return ExchangeResult.Failure(
                    ProviderName,
                    "Failed to deserialize response",
                    stopwatch.ElapsedMilliseconds);
            }

            // Calculate converted amount: amount * rate
            var convertedAmount = request.Amount * apiResponse.Rate;

            stopwatch.Stop();

            return ExchangeResult.Success(
                ProviderName,
                apiResponse.Rate,
                convertedAmount,
                stopwatch.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "{Provider} HTTP request failed", ProviderName);
            return ExchangeResult.Failure(ProviderName, $"HTTP request failed: {ex.Message}", stopwatch.ElapsedMilliseconds);
        }
        catch (JsonException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "{Provider} JSON parsing failed", ProviderName);
            return ExchangeResult.Failure(ProviderName, $"JSON parsing failed: {ex.Message}", stopwatch.ElapsedMilliseconds);
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "{Provider} request timed out", ProviderName);
            return ExchangeResult.Failure(ProviderName, "Request timed out", stopwatch.ElapsedMilliseconds);
        }
    }
}
