namespace ExchangeRateComparator.Domain.Entities;

/// <summary>
/// Represents the result from a single exchange rate provider
/// </summary>
public record ExchangeResult
{
    /// <summary>
    /// Gets whether the provider call was successful
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the exchange rate returned by the provider
    /// </summary>
    public decimal Rate { get; init; }

    /// <summary>
    /// Gets the converted amount
    /// </summary>
    public decimal ConvertedAmount { get; init; }

    /// <summary>
    /// Gets the name of the provider
    /// </summary>
    public string ProviderName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the error message if the call failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the execution time in milliseconds for this provider
    /// </summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static ExchangeResult Success(string providerName, decimal rate, decimal convertedAmount, long executionTimeMs)
    {
        return new ExchangeResult
        {
            IsSuccess = true,
            ProviderName = providerName,
            Rate = rate,
            ConvertedAmount = convertedAmount,
            ExecutionTimeMs = executionTimeMs
        };
    }

    /// <summary>
    /// Creates a failed result
    /// </summary>
    public static ExchangeResult Failure(string providerName, string errorMessage, long executionTimeMs)
    {
        return new ExchangeResult
        {
            IsSuccess = false,
            ProviderName = providerName,
            ErrorMessage = errorMessage,
            ExecutionTimeMs = executionTimeMs
        };
    }
}
