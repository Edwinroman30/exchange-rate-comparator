using ExchangeRateComparator.Domain.Entities;

namespace ExchangeRateComparator.Application.Interfaces;

/// <summary>
/// Defines the contract for the exchange rate comparator service
/// </summary>
public interface IExchangeRateComparatorService
{
    /// <summary>
    /// Queries all exchange rate providers in parallel and returns the best conversion rate
    /// </summary>
    /// <param name="request">The exchange request containing currencies and amount</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation</param>
    /// <returns>The best exchange rate result with provider information</returns>
    Task<ExchangeResponse> GetBestExchangeRateAsync(ExchangeRequest request, CancellationToken cancellationToken = default);
}
