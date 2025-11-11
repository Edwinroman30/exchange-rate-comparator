using System.Diagnostics;
using System.Text;
using System.Xml.Serialization;
using ExchangeRateComparator.Domain.Entities;
using ExchangeRateComparator.Domain.Interfaces;
using ExchangeRateComparator.Infrastructure.Models;
using Microsoft.Extensions.Logging;

namespace ExchangeRateComparator.Infrastructure.Adapters;

/// <summary>
/// Adapter for API 2 (XML/SOAP) - Returns direct converted amount
/// </summary>
public class Api2XmlAdapter : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<Api2XmlAdapter> _logger;

    public string ProviderName => "API2-XML";

    public Api2XmlAdapter(HttpClient httpClient, ILogger<Api2XmlAdapter> logger)
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
            var apiRequest = new Api2Request
            {
                From = request.SourceCurrency,
                To = request.TargetCurrency,
                Amount = request.Amount
            };

            var xml = SerializeToXml(apiRequest);
            var content = new StringContent(xml, Encoding.UTF8, "application/xml");

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

            var responseXml = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = DeserializeFromXml<Api2Response>(responseXml);

            if (apiResponse == null)
            {
                stopwatch.Stop();
                return ExchangeResult.Failure(
                    ProviderName,
                    "Failed to deserialize response",
                    stopwatch.ElapsedMilliseconds);
            }

            // API 2 returns the converted amount directly
            var convertedAmount = apiResponse.Result;
            
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
        catch (InvalidOperationException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "{Provider} XML parsing failed", ProviderName);
            return ExchangeResult.Failure(ProviderName, $"XML parsing failed: {ex.Message}", stopwatch.ElapsedMilliseconds);
        }
        catch (TaskCanceledException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "{Provider} request timed out", ProviderName);
            return ExchangeResult.Failure(ProviderName, "Request timed out", stopwatch.ElapsedMilliseconds);
        }
    }

    private static string SerializeToXml<T>(T obj)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var stringWriter = new StringWriter();
        serializer.Serialize(stringWriter, obj);
        return stringWriter.ToString();
    }

    private static T? DeserializeFromXml<T>(string xml)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var stringReader = new StringReader(xml);
        return (T?)serializer.Deserialize(stringReader);
    }
}
