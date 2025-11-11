namespace ExchangeRateComparator.Domain.Entities;

/// <summary>
/// Represents a currency exchange rate request
/// </summary>
public record ExchangeRequest(string SourceCurrency, string TargetCurrency, decimal Amount)
{
    /// <summary>
    /// Gets the source currency code (e.g., "USD")
    /// </summary>
    public string SourceCurrency { get; init; } = SourceCurrency;

    /// <summary>
    /// Gets the target currency code (e.g., "EUR")
    /// </summary>
    public string TargetCurrency { get; init; } = TargetCurrency;

    /// <summary>
    /// Gets the amount to convert
    /// </summary>
    public decimal Amount { get; init; } = Amount;
}
