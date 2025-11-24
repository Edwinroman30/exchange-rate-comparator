using System.Diagnostics;
using System.Text.Json;
using ExchangeRateComparator.Domain.Entities;
using ExchangeRateComparator.Domain.Interfaces;
using ExchangeRateComparator.Infrastructure.Configuration;
using ExchangeRateComparator.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExchangeRateComparator.Infrastructure.Adapters;

/// <summary>
/// Adapter for API 3 (JSON/REST) - Complex response structure
/// </summary>
public class Api3JsonAdapter : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Api3JsonAdapter> _logger;
    private readonly string _endpointPath;

    public string ProviderName => "API3-JSON";

    public Api3JsonAdapter(
        HttpClient httpClient, 
        ILogger<Api3JsonAdapter> logger,
        IOptions<ExchangeRateApiSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settings);

        _httpClient = httpClient;
        _logger = logger;
        _endpointPath = settings.Value.Api3.EndpointPath;
    }

    public async Task<ExchangeResult> GetExchangeRateAsync(ExchangeRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var apiRequest = new Api3Request
            {
                Exchange = new Api3ExchangeInfo
                {
                    SourceCurrency = request.SourceCurrency,
                    TargetCurrency = request.TargetCurrency,
                    Quantity = request.Amount
                }
            };

            var json = JsonSerializer.Serialize(apiRequest);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending request to {Provider} at {EndpointPath}", ProviderName, _endpointPath);

            var response = await _httpClient.PostAsync(_endpointPath, content, cancellationToken);

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
            var apiResponse = JsonSerializer.Deserialize<Api3Response>(responseJson);

            if (apiResponse == null)
            {
                stopwatch.Stop();
                return ExchangeResult.Failure(
                    ProviderName,
                    "Failed to deserialize response",
                    stopwatch.ElapsedMilliseconds);
            }

            // Check API-level status code
            if (apiResponse.StatusCode != 200)
            {
                stopwatch.Stop();
                return ExchangeResult.Failure(
                    ProviderName,
                    $"API returned error: {apiResponse.Message}",
                    stopwatch.ElapsedMilliseconds);
            }

            // API 3 returns the converted amount in data.total
            var convertedAmount = apiResponse.Data.Total;
            
            // Calculate rate from converted amount
            var rate = request.Amount > 0 ? convertedAmount / request.Amount : 0;

            stopwatch.Stop();

            return ExchangeResult.Success(
                ProviderName,
                rate,
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
