using System.Net;
using ExchangeRateComparator.Domain.Entities;
using ExchangeRateComparator.Infrastructure.Adapters;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;

namespace ExchangeRateComparator.Tests.Adapters;

public class Api2XmlAdapterTests
{
    private readonly Mock<ILogger<Api2XmlAdapter>> _loggerMock;
    private readonly MockHttpMessageHandler _mockHttp;

    public Api2XmlAdapterTests()
    {
        _loggerMock = new Mock<ILogger<Api2XmlAdapter>>();
        _mockHttp = new MockHttpMessageHandler();
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenApiReturnsSuccess_ShouldReturnSuccessResult()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);
        var responseXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <XML>
                <Result>850.00</Result>
            </XML>
            """;

        _mockHttp.When("https://api2.example.com/api/exchange")
            .Respond("application/xml", responseXml);

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api2.example.com/");

        var adapter = new Api2XmlAdapter(httpClient, _loggerMock.Object);

        // Act
        var result = await adapter.GetExchangeRateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ConvertedAmount.Should().Be(850M);
        result.Rate.Should().Be(0.85M); // 850 / 1000
        result.ProviderName.Should().Be("API2-XML");
        result.ExecutionTimeMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenApiReturns404_ShouldReturnFailureResult()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);

        _mockHttp.When("https://api2.example.com/api/exchange")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api2.example.com/");

        var adapter = new Api2XmlAdapter(httpClient, _loggerMock.Object);

        // Act
        var result = await adapter.GetExchangeRateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("404");
        result.ProviderName.Should().Be("API2-XML");
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenApiReturns500_ShouldReturnFailureResult()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);

        _mockHttp.When("https://api2.example.com/api/exchange")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api2.example.com/");

        var adapter = new Api2XmlAdapter(httpClient, _loggerMock.Object);

        // Act
        var result = await adapter.GetExchangeRateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("500");
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenApiReturnsMalformedXml_ShouldReturnFailureResult()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);
        var malformedXml = "<XML><Result>Not a number</Result></XML>";

        _mockHttp.When("https://api2.example.com/api/exchange")
            .Respond("application/xml", malformedXml);

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api2.example.com/");

        var adapter = new Api2XmlAdapter(httpClient, _loggerMock.Object);

        // Act
        var result = await adapter.GetExchangeRateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("XML parsing failed");
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenRequestTimesOut_ShouldReturnFailureResult()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);

        _mockHttp.When("https://api2.example.com/api/exchange")
            .Respond(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api2.example.com/");
        httpClient.Timeout = TimeSpan.FromMilliseconds(100);

        var adapter = new Api2XmlAdapter(httpClient, _loggerMock.Object);

        // Act
        var result = await adapter.GetExchangeRateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("timed out");
    }

    [Theory]
    [InlineData(1000, 850, 0.85)]
    [InlineData(1000, 870, 0.87)]
    [InlineData(5000, 4600, 0.92)]
    [InlineData(100, 85, 0.85)]
    public async Task GetExchangeRateAsync_ShouldCalculateRateCorrectly(
        decimal amount, decimal convertedAmount, decimal expectedRate)
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", amount);
        var responseXml = $$"""
            <?xml version="1.0" encoding="utf-8"?>
            <XML>
                <Result>{{convertedAmount}}</Result>
            </XML>
            """;

        _mockHttp.When("https://api2.example.com/api/exchange")
            .Respond("application/xml", responseXml);

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api2.example.com/");

        var adapter = new Api2XmlAdapter(httpClient, _loggerMock.Object);

        // Act
        var result = await adapter.GetExchangeRateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ConvertedAmount.Should().Be(convertedAmount);
        result.Rate.Should().Be(expectedRate);
    }

    [Fact]
    public void ProviderName_ShouldReturnCorrectName()
    {
        // Arrange
        var httpClient = _mockHttp.ToHttpClient();
        var adapter = new Api2XmlAdapter(httpClient, _loggerMock.Object);

        // Act & Assert
        adapter.ProviderName.Should().Be("API2-XML");
    }

    [Fact]
    public void Constructor_WhenHttpClientIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new Api2XmlAdapter(null!, _loggerMock.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var httpClient = _mockHttp.ToHttpClient();

        // Act & Assert
        var act = () => new Api2XmlAdapter(httpClient, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var httpClient = _mockHttp.ToHttpClient();
        var adapter = new Api2XmlAdapter(httpClient, _loggerMock.Object);

        // Act & Assert
        var act = async () => await adapter.GetExchangeRateAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
