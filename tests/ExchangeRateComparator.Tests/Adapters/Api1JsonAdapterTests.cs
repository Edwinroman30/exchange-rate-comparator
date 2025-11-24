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

public class Api1JsonAdapterTests
{
    private readonly Mock<ILogger<Api1JsonAdapter>> _loggerMock;
    private readonly MockHttpMessageHandler _mockHttp;

    public Api1JsonAdapterTests()
    {
        _loggerMock = new Mock<ILogger<Api1JsonAdapter>>();
        _mockHttp = new MockHttpMessageHandler();
    }

    private static IOptions<ExchangeRateApiSettings> CreateMockSettings(string endpointPath = "/api/exchange")
    {
        var settings = new ExchangeRateApiSettings
        {
            Api1 = new ApiEndpointSettings
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
        var responseJson = """{"rate": 0.85}""";

        _mockHttp.When("https://api1.example.com/api/exchange")
            .Respond("application/json", responseJson);

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api1.example.com/");

        var adapter = new Api1JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

        // Act
        var result = await adapter.GetExchangeRateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Rate.Should().Be(0.85M);
        result.ConvertedAmount.Should().Be(850M); // 1000 * 0.85
        result.ProviderName.Should().Be("API1-JSON");
        result.ExecutionTimeMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenApiReturns404_ShouldReturnFailureResult()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);

        _mockHttp.When("https://api1.example.com/api/exchange")
            .Respond(HttpStatusCode.NotFound);

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api1.example.com/");

        var adapter = new Api1JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

        // Act
        var result = await adapter.GetExchangeRateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("404");
        result.ProviderName.Should().Be("API1-JSON");
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenApiReturns500_ShouldReturnFailureResult()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);

        _mockHttp.When("https://api1.example.com/api/exchange")
            .Respond(HttpStatusCode.InternalServerError);

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api1.example.com/");

        var adapter = new Api1JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

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
        var malformedJson = "{invalid json}";

        _mockHttp.When("https://api1.example.com/api/exchange")
            .Respond("application/json", malformedJson);

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api1.example.com/");

        var adapter = new Api1JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

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

        _mockHttp.When("https://api1.example.com/api/exchange")
            .Respond(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api1.example.com/");
        httpClient.Timeout = TimeSpan.FromMilliseconds(100);

        var adapter = new Api1JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

        // Act
        var result = await adapter.GetExchangeRateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("timed out");
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenCancellationRequested_ShouldReturnFailureResult()
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", 1000M);
        var cts = new CancellationTokenSource();

        _mockHttp.When("https://api1.example.com/api/exchange")
            .Respond(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api1.example.com/");

        var adapter = new Api1JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

        // Cancel immediately
        cts.Cancel();

        // Act
        var result = await adapter.GetExchangeRateAsync(request, cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    [Theory]
    [InlineData(100, 0.85, 85)]
    [InlineData(1000, 0.85, 850)]
    [InlineData(5000, 0.92, 4600)]
    [InlineData(0.5, 0.85, 0.425)]
    public async Task GetExchangeRateAsync_ShouldCalculateConvertedAmountCorrectly(
        decimal amount, decimal rate, decimal expectedConverted)
    {
        // Arrange
        var request = new ExchangeRequest("USD", "EUR", amount);
        var responseJson = $$"""{"rate": {{rate}}}""";

        _mockHttp.When("https://api1.example.com/api/exchange")
            .Respond("application/json", responseJson);

        var httpClient = _mockHttp.ToHttpClient();
        httpClient.BaseAddress = new Uri("https://api1.example.com/");

        var adapter = new Api1JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

        // Act
        var result = await adapter.GetExchangeRateAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Rate.Should().Be(rate);
        result.ConvertedAmount.Should().Be(expectedConverted);
    }

    [Fact]
    public void ProviderName_ShouldReturnCorrectName()
    {
        // Arrange
        var httpClient = _mockHttp.ToHttpClient();
        var adapter = new Api1JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

        // Act & Assert
        adapter.ProviderName.Should().Be("API1-JSON");
    }

    [Fact]
    public void Constructor_WhenHttpClientIsNull_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new Api1JsonAdapter(null!, _loggerMock.Object, CreateMockSettings());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var httpClient = _mockHttp.ToHttpClient();

        // Act & Assert
        var act = () => new Api1JsonAdapter(httpClient, null!, CreateMockSettings());
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WhenSettingsIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var httpClient = _mockHttp.ToHttpClient();

        // Act & Assert
        var act = () => new Api1JsonAdapter(httpClient, _loggerMock.Object, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task GetExchangeRateAsync_WhenRequestIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var httpClient = _mockHttp.ToHttpClient();
        var adapter = new Api1JsonAdapter(httpClient, _loggerMock.Object, CreateMockSettings());

        // Act & Assert
        var act = async () => await adapter.GetExchangeRateAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
