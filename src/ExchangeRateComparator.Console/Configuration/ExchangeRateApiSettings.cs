namespace ExchangeRateComparator.Console.Configuration;

/// <summary>
/// Configuration settings for exchange rate API providers
/// </summary>
public class ExchangeRateApiSettings
{
    public const string SectionName = "ExchangeRateApis";

    /// <summary>
    /// Configuration for API 1 (JSON/REST)
    /// </summary>
    public ApiEndpointSettings Api1 { get; set; } = new();

    /// <summary>
    /// Configuration for API 2 (XML/SOAP)
    /// </summary>
    public ApiEndpointSettings Api2 { get; set; } = new();

    /// <summary>
    /// Configuration for API 3 (Complex JSON)
    /// </summary>
    public ApiEndpointSettings Api3 { get; set; } = new();
}

/// <summary>
/// Configuration for an individual API endpoint
/// </summary>
public class ApiEndpointSettings
{
    /// <summary>
    /// Base URL of the API
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Timeout in seconds for API requests
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Whether this API is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;
}
