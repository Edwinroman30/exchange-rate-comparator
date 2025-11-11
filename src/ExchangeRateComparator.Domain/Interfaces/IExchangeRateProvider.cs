using ExchangeRateComparator.Domain.Entities;

namespace ExchangeRateComparator.Domain.Interfaces;

/// <summary>
/// Defines a contract for exchange rate providers
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>
    /// Gets the name of the provider
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Retrieves the exchange rate for the specified request
    /// </summary>
    /// <param name="request">The exchange request containing source currency, target currency, and amount</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The exchange result with rate and converted amount</returns>
    Task<ExchangeResult> GetExchangeRateAsync(ExchangeRequest request, CancellationToken cancellationToken = default);
}
