using System.Net;
using ExchangeRateComparator.Domain.Entities;
using ExchangeRateComparator.Infrastructure.Adapters;
using ExchangeRateComparator.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;

namespace ExchangeRateComparator.Tests.Adapters;

public class Api3JsonAdapterTests
{
    private readonly Mock<ILogger<Api3JsonAdapter>> _loggerMock;
    private readonly MockHttpMessageHandler _mockHttp;

    public Api3JsonAdapterTests()
    {
        _loggerMock = new Mock<ILogger<Api3JsonAdapter>>();
        _mockHttp = new MockHttpMessageHandler();
    }

    private static IOptions<ExchangeRateApiSettings> CreateMockSettings(string endpointPath = "/api/exchange")
    {
        var settings = new ExchangeRateApiSettings
        {
            Api3 = new ApiEndpointSettings
            {
                BaseUrl = "http://localhost:5298/",
                EndpointPath = endpointPath,
                TimeoutSeconds = 10,
                Enabled = true
            }
        };
        return Options.Create(settings);
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenApiReturnsSuccess_ShouldReturnSuccessResult()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);
        var responseJson = """
            {
                "statusCode": 200,
                "message": "Success",
                "data": {
                    "total": 850.00
                }
            }
            """;

        _mockHttp.When("https://api3.example.com/api/exchange")
            .Respond("application/json", responseJson);

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api3.example.com/");

        var adapter = new Api3JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

        // Act
        var result = await adapter.GetExchangeRateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.ConvertedAmount.Should().Be(850M);
        result.Rate.Should().Be(0.85M); // 850 / 1000
        result.ProviderName.Should().Be("API3-JSON");
        result.ExecutionTimeMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenApiReturnsErrorStatusCode_ShouldReturnFailureResult()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);
        var responseJson = """
            {
                "statusCode": 400,
                "message": "Invalid currency code",
                "data": {
                    "total": 0
                }
            }
            """;

        _mockHttp.When("https://api3.example.com/api/exchange")
            .Respond("application/json", responseJson);

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api3.example.com/");

        var adapter = new Api3JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

        // Act
        var result = await adapter.GetExchangeRateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid currency code");
        result.ProviderName.Should().Be("API3-JSON");
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenHttpReturns404_ShouldReturnFailureResult()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);

        _mockHttp.When("https://api3.example.com/api/exchange")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api3.example.com/");

        var adapter = new Api3JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

        // Act
        var result = await adapter.GetExchangeRateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("404");
        result.ProviderName.Should().Be("API3-JSON");
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenHttpReturns500_ShouldReturnFailureResult()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);

        _mockHttp.When("https://api3.example.com/api/exchange")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api3.example.com/");

        var adapter = new Api3JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

        // Act
        var result = await adapter.GetExchangeRateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("500");
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenApiReturnsMalformedJson_ShouldReturnFailureResult()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);
        var malformedJson = "{invalid json structure}";

        _mockHttp.When("https://api3.example.com/api/exchange")
            .Respond("application/json", malformedJson);

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api3.example.com/");

        var adapter = new Api3JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

        // Act
        var result = await adapter.GetExchangeRateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("JSON parsing failed");
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenRequestTimesOut_ShouldReturnFailureResult()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);

        _mockHttp.When("https://api3.example.com/api/exchange")
            .Respond(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api3.example.com/");
        httpClient.Timeout = TimeSpan.FromMilliseconds(100);

        var adapter = new Api3JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

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
        var responseJson = $$"""
            {
                "statusCode": 200,
                "message": "Success",
                "data": {
                    "total": {{convertedAmount}}
                }
            }
            """;

        _mockHttp.When("https://api3.example.com/api/exchange")
            .Respond("application/json", responseJson);

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api3.example.com/");

        var adapter = new Api3JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

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
        var adapter = new Api3JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

        // Act & Assert
        adapter.ProviderName.Should().Be("API3-JSON");
    }

    [Fact]
    public void Constructor_WhenHttpClientIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new Api3JsonAdapter(null!, _loggerMock.Object, CreateMockSettings());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var httpClient = _mockHttp.ToHttpClient();

        // Act & Assert
        var act = () => new Api3JsonAdapter(httpClient, null!, CreateMockSettings());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WhenSettingsIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var httpClient = _mockHttp.ToHttpClient();

        // Act & Assert
        var act = () => new Api3JsonAdapter(httpClient, _loggerMock.Object, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var httpClient = _mockHttp.ToHttpClient();
        var adapter = new Api3JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

        // Act & Assert
        var act = async () => await adapter.GetExchangeRateAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
