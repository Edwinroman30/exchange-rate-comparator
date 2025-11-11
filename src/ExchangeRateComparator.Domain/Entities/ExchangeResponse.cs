namespace ExchangeRateComparator.Domain.Entities;

/// <summary>
/// Represents the result of an exchange rate comparison query
/// </summary>
public record ExchangeResponse
{
    /// <summary>
    /// Gets the best exchange rate found across all providers
    /// </summary>
    public decimal BestRate { get; init; }

    /// <summary>
    /// Gets the converted amount using the best rate
    /// </summary>
    public decimal ConvertedAmount { get; init; }

    /// <summary>
    /// Gets the name of the provider offering the best rate
    /// </summary>
    public string Provider { get; init; } = string.Empty;

    /// <summary>
    /// Gets the total execution time in milliseconds
    /// </summary>
    public long ExecutionTimeMs { get; init; }
}
