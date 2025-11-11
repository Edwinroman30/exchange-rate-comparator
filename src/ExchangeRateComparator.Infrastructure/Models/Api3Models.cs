using System.Text.Json.Serialization;

namespace ExchangeRateComparator.Infrastructure.Models;

/// <summary>
/// Request model for API 3 (JSON/REST)
/// </summary>
internal record Api3Request
{
    [JsonPropertyName("exchange")]
    public Api3ExchangeInfo Exchange { get; init; } = new();
}

internal record Api3ExchangeInfo
{
    [JsonPropertyName("sourceCurrency")]
    public string SourceCurrency { get; init; } = string.Empty;

    [JsonPropertyName("targetCurrency")]
    public string TargetCurrency { get; init; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; init; }
}

/// <summary>
/// Response model for API 3 (JSON/REST)
/// </summary>
internal record Api3Response
{
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; init; }

    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    [JsonPropertyName("data")]
    public Api3Data Data { get; init; } = new();
}

internal record Api3Data
{
    [JsonPropertyName("total")]
    public decimal Total { get; init; }
}
