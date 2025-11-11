using System.Text.Json.Serialization;

namespace ExchangeRateComparator.Infrastructure.Models;

/// <summary>
/// Request model for API 1 (JSON/REST)
/// </summary>
internal record Api1Request
{
    [JsonPropertyName("from")]
    public string From { get; init; } = string.Empty;

    [JsonPropertyName("to")]
    public string To { get; init; } = string.Empty;

    [JsonPropertyName("value")]
    public decimal Value { get; init; }
}

/// <summary>
/// Response model for API 1 (JSON/REST)
/// </summary>
internal record Api1Response
{
    [JsonPropertyName("rate")]
    public decimal Rate { get; init; }
}
