using ExchangeRateComparator.Application.Services;
using ExchangeRateComparator.Domain.Entities;
using ExchangeRateComparator.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExchangeRateComparator.Tests.Services;

public class ExchangeRateComparatorServiceTests
{
    private readonly Mock<IExchangeRateProvider> _provider1Mock;
    private readonly Mock<IExchangeRateProvider> _provider2Mock;
    private readonly Mock<IExchangeRateProvider> _provider3Mock;
    private readonly Mock<ILogger<ExchangeRateComparatorService>> _loggerMock;

    public ExchangeRateComparatorServiceTests()
    {
        _provider1Mock = new Mock<IExchangeRateProvider>();
        _provider2Mock = new Mock<IExchangeRateProvider>();
        _provider3Mock = new Mock<IExchangeRateProvider>();
        _loggerMock = new Mock<ILogger<ExchangeRateComparatorService>>();

        _provider1Mock.Setup(p => p.ProviderName).Returns("Provider1");
        _provider2Mock.Setup(p => p.ProviderName).Returns("Provider2");
        _provider3Mock.Setup(p => p.ProviderName).Returns("Provider3");
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WhenAllProvidersSucceed_ShouldReturnHighestConversion()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);

        _provider1Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Success("Provider1", 0.85M, 850M, 100));

        _provider2Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Success("Provider2", 0.87M, 870M, 120));

        _provider3Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Success("Provider3", 0.86M, 860M, 110));

        var providers = new[] { _provider1Mock.Object, _provider2Mock.Object, _provider3Mock.Object };
        var service = new ExchangeRateComparatorService(providers, _loggerMock.Object);

        // Act
        var result = await service.GetBestExchangeRateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ConvertedAmount.Should().Be(870M);
        result.BestRate.Should().Be(0.87M);
        result.Provider.Should().Be("Provider2");
        result.ExecutionTimeMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WhenOneProviderFails_ShouldReturnBestFromRemaining()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);

        _provider1Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Failure("Provider1", "Connection failed", 50));

        _provider2Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Success("Provider2", 0.87M, 870M, 120));

        _provider3Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Success("Provider3", 0.86M, 860M, 110));

        var providers = new[] { _provider1Mock.Object, _provider2Mock.Object, _provider3Mock.Object };
        var service = new ExchangeRateComparatorService(providers, _loggerMock.Object);

        // Act
        var result = await service.GetBestExchangeRateAsync(request);

        // Assert
        result.ConvertedAmount.Should().Be(870M);
        result.Provider.Should().Be("Provider2");
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WhenTwoProvidersFail_ShouldReturnLastSuccessful()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);

        _provider1Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Failure("Provider1", "Timeout", 5000));

        _provider2Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Failure("Provider2", "API Error 500", 200));

        _provider3Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Success("Provider3", 0.86M, 860M, 110));

        var providers = new[] { _provider1Mock.Object, _provider2Mock.Object, _provider3Mock.Object };
        var service = new ExchangeRateComparatorService(providers, _loggerMock.Object);

        // Act
        var result = await service.GetBestExchangeRateAsync(request);

        // Assert
        result.ConvertedAmount.Should().Be(860M);
        result.Provider.Should().Be("Provider3");
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WhenAllProvidersFail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);

        _provider1Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Failure("Provider1", "Connection failed", 100));

        _provider2Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Failure("Provider2", "Timeout", 5000));

        _provider3Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Failure("Provider3", "API Error", 200));

        var providers = new[] { _provider1Mock.Object, _provider2Mock.Object, _provider3Mock.Object };
        var service = new ExchangeRateComparatorService(providers, _loggerMock.Object);

        // Act & Assert
        var act = async () => await service.GetBestExchangeRateAsync(request);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("All exchange rate providers failed. Please try again later.");
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WhenProviderThrowsException_ShouldHandleGracefully()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);

        _provider1Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        _provider2Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Success("Provider2", 0.87M, 870M, 120));

        var providers = new[] { _provider1Mock.Object, _provider2Mock.Object };
        var service = new ExchangeRateComparatorService(providers, _loggerMock.Object);

        // Act
        var result = await service.GetBestExchangeRateAsync(request);

        // Assert
        result.ConvertedAmount.Should().Be(870M);
        result.Provider.Should().Be("Provider2");
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WhenCancellationRequested_ShouldHandleGracefully()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _provider1Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        _provider2Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Success("Provider2", 0.87M, 870M, 120));

        var providers = new[] { _provider1Mock.Object, _provider2Mock.Object };
        var service = new ExchangeRateComparatorService(providers, _loggerMock.Object);

        // Act
        var result = await service.GetBestExchangeRateAsync(request, cts.Token);

        // Assert
        result.ConvertedAmount.Should().Be(870M);
        result.Provider.Should().Be("Provider2");
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_ShouldSelectHighestConvertedAmount()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);

        // Even though Provider3 has lower rate, it has higher converted amount
        _provider1Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Success("Provider1", 0.85M, 850M, 100));

        _provider2Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Success("Provider2", 0.86M, 860M, 120));

        _provider3Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Success("Provider3", 0.84M, 875M, 110));

        var providers = new[] { _provider1Mock.Object, _provider2Mock.Object, _provider3Mock.Object };
        var service = new ExchangeRateComparatorService(providers, _loggerMock.Object);

        // Act
        var result = await service.GetBestExchangeRateAsync(request);

        // Assert
        result.ConvertedAmount.Should().Be(875M);
        result.BestRate.Should().Be(0.84M);
        result.Provider.Should().Be("Provider3");
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WhenConvertedAmountsAreEqual_ShouldSelectFastestProvider()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);

        // All providers return same converted amount, but different execution times
        _provider1Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Success("Provider1", 0.85M, 850M, 150));

        _provider2Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Success("Provider2", 0.85M, 850M, 80));

        _provider3Mock
            .Setup(p => p.GetExchangeRateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExchangeResult.Success("Provider3", 0.85M, 850M, 120));

        var providers = new[] { _provider1Mock.Object, _provider2Mock.Object, _provider3Mock.Object };
        var service = new ExchangeRateComparatorService(providers, _loggerMock.Object);

        // Act
        var result = await service.GetBestExchangeRateAsync(request);

        // Assert
        result.ConvertedAmount.Should().Be(850M);
        result.BestRate.Should().Be(0.85M);
        result.Provider.Should().Be("Provider2", "it has the fastest execution time (80ms)");
    }

    [Theory]
    [InlineData(null, "EUR", 1000)]
    [InlineData("", "EUR", 1000)]
    [InlineData("  ", "EUR", 1000)]
    [InlineData("USD", null, 1000)]
    [InlineData("USD", "", 1000)]
    [InlineData("USD", "  ", 1000)]
    public async Task GetBestExchangeRateAsync_WhenCurrencyIsInvalid_ShouldThrowArgumentException(
        string? sourceCurrency, string? targetCurrency, decimal amount)
    {
        // Arrange
        var request = new ExchangeRequest(sourceCurrency!, targetCurrency!, amount);
        var service = new ExchangeRateComparatorService(
            new[] { _provider1Mock.Object }, 
            _loggerMock.Object);

        // Act & Assert
        var act = async () => await service.GetBestExchangeRateAsync(request);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WhenAmountIsZeroOrNegative_ShouldThrowArgumentException()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 0M);
        var service = new ExchangeRateComparatorService(
            new[] { _provider1Mock.Object }, 
            _loggerMock.Object);

        // Act & Assert
        var act = async () => await service.GetBestExchangeRateAsync(request);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Amount must be greater than zero*");
    }

    [Fact]
    public void Constructor_WhenProvidersIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ExchangeRateComparatorService(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ExchangeRateComparatorService(
            new[] { _provider1Mock.Object }, 
            null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var service = new ExchangeRateComparatorService(
            new[] { _provider1Mock.Object }, 
            _loggerMock.Object);

        // Act & Assert
        var act = async () => await service.GetBestExchangeRateAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
