# 📊 Exchange Rate Comparator Service

A high-performance currency exchange rate comparison service built with .NET 9 that compares multiple currency exchange APIs in parallel and selects the best rate.
Clean Architecture | SOLID | Resilient | Well-Tested

⚠️ Note: Contains design decisions documentation for ambiguous requirements.
See README for interpretation details.


## 🎯 Features

- **Parallel API Execution**: Queries multiple exchange rate APIs concurrently using `Task.WhenAll()`
- Query multiple exchange rate APIs with different signatures

- **Graceful Degradation**: Continues functioning even if some APIs fail- Return the highest conversion amount

- **Best Rate Selection**: Automatically selects the highest conversion amount- Minimize response time using async/await and parallel execution

- **Clean Architecture**: Separation of concerns with Domain, Application, and Infrastructure layers- Handle API failures gracefully

- **Comprehensive Error Handling**: Timeout handling, network error handling, and malformed response handling- No UI or SQL database required

- **Structured Logging**: Detailed logging using Microsoft.Extensions.Logging

- Console application or Class Library with unit tests

- **Well-Tested**: 58+ unit tests with xUnit, Moq, and FluentAssertions

- **SOLID Principles**: Follows all five SOLID principles for maintainability

## Conditions :


- No UI expected.  
- No SQL required. 
- Must be unit-tested. 
- Must work even if one or more APIs are unavailable or return invalid responses. 
• Good practices expected (e.g., clean architecture, SOLID principles, logging, error 
handling). 

## Functional Requirements:

### Process Input:

* one set of data {source currency, target currency, amount} 
* Multiple API using the same data with different signatures

```csharp
public class ExchangeRequest
{
    public string SourceCurrency { get; set; }  // e.g., "USD"
    public string TargetCurrency { get; set; }  // e.g., "EUR"
    public decimal Amount { get; set; }         // e.g., 1000.00M
}
```

### Process Output: 
* All API respond with the same data in different formats 
* Process must query, then select the highest conversion amount and return it in the least amount of time

Sample APIs, each with its own url and credentials:

## 📦 API Specifications

```
API1 (JSON) 
- Input {from, to, value} 
- Output {rate} 

API2 (XML) 
- Input <XML><From/><To/><Amount/></XML> 
- Output <XML><Result/></XML> 

API3 (JSON) 
- Input {exchange: {sourceCurrency, targetCurrency, quantity}} 
- Output {statusCode, message, data: {total}}
```

 [📋 See here the formal requirements document.](./doc/Technical%20test%20-%20Exchange%20Rate%20Offers%20for%20Banking%20Clients.pdf)


## ⚠️ Important: Requirements Interpretation & Design Decisions (MUST READ)

[The original requirements document](./doc/Technical%20test%20-%20Exchange%20Rate%20Offers%20for%20Banking%20Clients.pdf) contains some ambiguities regarding API response formats. 

Here's how I interpreted and implemented the solution:

### Ambiguity Identified
The document states:
> "All API respond with the same data in different formats"

However, the output field names suggest different response types:
- **API1** returns `rate` (typically an exchange rate like 0.85)
- **API2** returns `Result` (could be rate or converted amount)
- **API3** returns `data.total` (typically a total/converted amount)

### My Interpretation
Since the requirement explicitly states to **"select the highest conversion amount"**, 
this implies different APIs return **different exchange rates**, making comparison meaningful.

I implemented the following logic:

| API | Returns | Adapter Logic |
|-----|---------|---------------|
| **API1** | Exchange rate (e.g., `0.85`) | `ConvertedAmount = Amount × Rate` |
| **API2** | Converted amount directly (e.g., `870`) | `Rate = Result ÷ Amount` |
| **API3** | Converted amount in nested structure | `Rate = data.total ÷ Amount` |

### Rationale
- Each API has its own data source/rate provider
- Different providers offer different rates (competitive market scenario)
- The service compares results and selects the best offer for the client
- This interpretation makes the "comparison" requirement meaningful

### Alternative Interpretation Considered
If all APIs returned identical converted amounts (same rate, same result), there would be 
no "best" to select, making the comparison logic unnecessary. This seemed inconsistent 
with the project goal.

### 🧪 Mock API Implementation

For demonstration and testing purposes, the Mock API server returns **different rates** for each endpoint:

| Endpoint | Typical Rate Range | Purpose |
|----------|-------------------|---------|
| **API1** (`/api/exchange`) | ~1.18 | Usually returns the **best rate** |
| **API2** (`/api/exchange/xml`) | ~1.15 | Medium rate |
| **API3** (`/api/exchange/complex`) | ~1.16 | Variable rate |

**Rates are randomized** on each call to simulate real market fluctuations.

**Example Test Scenario:**
```
Input: Convert $1000 USD to EUR
- API1 returns rate 1.18 → €1,180.00 ✅ BEST
- API2 returns €1,150.00 (rate 1.15)
- API3 returns €1,160.00 (rate 1.16)
Result: Service selects API1 (highest conversion amount)
```

This simulates real-world scenarios where different providers offer different exchange rates.

---

**If this interpretation doesn't match your intended requirements, please refer to the configuration section below to adjust the API behavior or replace the mock APIs with your own implementations.**

---



## 🚀 Quick Start

### 📋 Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- (Optional) [Docker](https://www.docker.com/) for containerized deployment

### 📥 Clone the Repository
```bash
git clone https://github.com/Edwinroman30/exchange-rate-comparator.git
cd exchange-rate-comparator
```

### 🔧 Configuration Setup (⚠️ IMPORTANT - READ FIRST)

The application uses **IOptions pattern** with `appsettings.json` for configuration. You have **two options**:

#### **Option 1: Use the Included Mock API Server (Recommended for Testing)** 🎯

Perfect for evaluation, testing, and demonstration purposes.

**Step 1:** Start the Mock API Server
```bash
# In Terminal 1
cd src/ExchangeRateComparator.MockApi
dotnet run
```

The Mock API will start on `http://localhost:5298` (or the port shown in the terminal).

**Step 2:** Update the Console application configuration

Edit `src/ExchangeRateComparator.Console/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "ExchangeRateApis": {
    "Api1": {
      "BaseUrl": "http://localhost:5298/",
      "TimeoutSeconds": 10,
      "Enabled": true
    },
    "Api2": {
      "BaseUrl": "http://localhost:5298/",
      "TimeoutSeconds": 10,
      "Enabled": true
    },
    "Api3": {
      "BaseUrl": "http://localhost:5298/",
      "TimeoutSeconds": 10,
      "Enabled": true
    }
  }
}
```

✅ **Note:** All three APIs point to the same Mock server, which provides different endpoints:
- `/api/exchange` (API1 - JSON with rate only)
- `/api/exchange/xml` (API2 - XML with converted amount)
- `/api/exchange/complex` (API3 - Complex JSON with nested structure)

**Step 3:** Run the Console Application
```bash
# In Terminal 2
cd src/ExchangeRateComparator.Console
dotnet run
```

---

#### **Option 2: Use Your Own APIs** 🌐

For production or testing with real exchange rate providers.

**Step 1:** Edit `src/ExchangeRateComparator.Console/appsettings.json`

```json
{
  "ExchangeRateApis": {
    "Api1": {
      "BaseUrl": "https://your-api1-endpoint.com/",
      "TimeoutSeconds": 10,
      "Enabled": true
    },
    "Api2": {
      "BaseUrl": "https://your-api2-endpoint.com/",
      "TimeoutSeconds": 15,
      "Enabled": true
    },
    "Api3": {
      "BaseUrl": "https://your-api3-endpoint.com/",
      "TimeoutSeconds": 10,
      "Enabled": false  // Disable if not available
    }
  }
}
```

**Configuration Properties:**
- `BaseUrl`: The base URL of your API endpoint (must end with `/`)
- `TimeoutSeconds`: HTTP request timeout in seconds
- `Enabled`: Set to `false` to disable a specific API (useful if some providers are unavailable)

**API Contract Requirements:**

Your APIs must implement these endpoints:

**API1 - JSON Format:**
```http
POST /api/exchange
Content-Type: application/json

{
  "from": "USD",
  "to": "EUR",
  "value": 1000.00
}

Response:
{
  "rate": 0.85
}
```

**API2 - XML Format:**
```http
POST /api/exchange/xml
Content-Type: text/xml

<ExchangeRequest>
  <From>USD</From>
  <To>EUR</To>
  <Amount>1000.00</Amount>
</ExchangeRequest>

Response:
<ExchangeResponse>
  <Result>850.00</Result>
</ExchangeResponse>
```

**API3 - Complex JSON Format:**
```http
POST /api/exchange/complex
Content-Type: application/json

{
  "exchange": {
    "sourceCurrency": "USD",
    "targetCurrency": "EUR",
    "quantity": 1000.00
  }
}

Response:
{
  "statusCode": 200,
  "message": "Success",
  "data": {
    "total": 850.00
  }
}
```

**Step 2:** Run the Console Application
```bash
cd src/ExchangeRateComparator.Console
dotnet run
```

---

### 🔐 Environment-Specific Configuration

You can override settings per environment using `appsettings.{Environment}.json`:

**Development Environment:**
```bash
# Set environment variable
export DOTNET_ENVIRONMENT=Development  # Linux/Mac
$env:DOTNET_ENVIRONMENT = "Development"  # PowerShell

# Run with development settings
dotnet run
```

**Production Environment:**
```bash
export DOTNET_ENVIRONMENT=Production
dotnet run
```

The application will automatically load:
1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (environment-specific overrides)
3. Environment variables (highest priority)

---

### 🏗️ Build the Solution

```bash
dotnet build
```

### ▶️ Run the Console Application

```bash
dotnet run --project src/ExchangeRateComparator.Console
```

## 🏗️ Architecture

The solution follows **Clean Architecture** principles with clear separation of concerns:

```
ExchangeRateComparator/
├── src/
│   ├── ExchangeRateComparator.Domain/          # Core Business Logic
│   │   ├── Entities/
│   │   │   ├── ExchangeRequest.cs              # Input model
│   │   │   ├── ExchangeResponse.cs             # Output model
│   │   │   └── ExchangeResult.cs               # Aggregated result
│   │   └── Interfaces/
│   │       └── IExchangeRateProvider.cs        # Provider contract
│   │
│   ├── ExchangeRateComparator.Application/     # Use Cases & Services
│   │   ├── Services/
│   │   │   └── ExchangeRateComparatorService.cs # Core comparison logic
│   │   └── Interfaces/
│   │       └── IExchangeRateComparatorService.cs
│   │
│   ├── ExchangeRateComparator.Infrastructure/  # External Dependencies
│   │   ├── Adapters/
│   │   │   ├── Api1JsonAdapter.cs              # JSON/REST API adapter
│   │   │   ├── Api2XmlAdapter.cs               # XML/SOAP API adapter
│   │   │   └── Api3JsonAdapter.cs              # Complex JSON adapter
│   │   └── Models/
│   │       ├── Api1Models.cs                   # API1 request/response DTOs
│   │       ├── Api2Models.cs                   # API2 XML models
│   │       └── Api3Models.cs                   # API3 nested DTOs
│   │
│   ├── ExchangeRateComparator.Console/         # Entry Point
│   │   ├── Program.cs                          # DI configuration & execution
│   │   ├── Configuration/
│   │   │   └── ExchangeRateApiSettings.cs      # IOptions configuration model
│   │   ├── appsettings.json                    # Base configuration
│   │   └── appsettings.Development.json        # Dev environment config
│   │
│   └── ExchangeRateComparator.MockApi/         # 🆕 Mock API Server
│       ├── Program.cs                          # Minimal API endpoints
│       └── Properties/
│           └── launchSettings.json             # Port configuration
│
└── tests/
    └── ExchangeRateComparator.Tests/           # Unit Tests (58+ tests)
        ├── Services/
        │   └── ExchangeRateComparatorServiceTests.cs
        └── Adapters/
            ├── Api1JsonAdapterTests.cs
            ├── Api2XmlAdapterTests.cs
            └── Api3JsonAdapterTests.cs
```

### 🎨 Architecture Highlights

**Domain Layer (Core)**
- ✅ No external dependencies
- ✅ Contains business entities and interfaces
- ✅ Defines contracts for other layers

**Application Layer**
- ✅ Implements use cases and business logic
- ✅ Orchestrates parallel API calls
- ✅ Depends only on Domain layer

**Infrastructure Layer**
- ✅ Implements domain interfaces
- ✅ Handles external API communication
- ✅ Adapts different API formats to domain models
- ✅ Uses HttpClient with IHttpClientFactory

**Console Layer (Presentation)**
- ✅ Entry point with dependency injection
- ✅ **IOptions pattern** for type-safe configuration
- ✅ Environment-specific settings support
- ✅ Configurable API endpoints and timeouts

**Mock API Server** 🆕
- ✅ ASP.NET Core Minimal API
- ✅ Three endpoints matching specifications
- ✅ Randomized rates for realistic testing
- ✅ No database required
        └── Adapters/
            ├── Api1JsonAdapterTests.cs
            ├── Api2XmlAdapterTests.cs
            └── Api3JsonAdapterTests.cs
```


## 🧪 Testing

The project includes comprehensive unit tests using:
- **xUnit**: Test framework
- **Moq**: Mocking framework
- **FluentAssertions**: Assertion library
- **MockHttp**: HTTP client mocking

### Run Tests with Coverage

```bash
dotnet test
```

### Test Coverage

- **Total Tests**: 58+
- **All Tests Passing**: ✅
- **Coverage**: Critical paths covered including:
  - Success scenarios for all APIs
  - Failure scenarios (404, 500, timeouts)
  - Malformed response handling
  - All providers failing
  - Mixed success/failure scenarios
  - Input validation
  - Null checks


## 🔧 Configuration

The application uses **IOptions pattern** with `appsettings.json` for type-safe, environment-aware configuration.

### Configuration Structure

```json
{
  "ExchangeRateApis": {
    "Api1": {
      "BaseUrl": "http://localhost:5298/",
      "TimeoutSeconds": 10,
      "Enabled": true
    },
    "Api2": { /* ... */ },
    "Api3": { /* ... */ }
  }
}
```

### Key Features
- ✅ **Type-Safe:** Strongly typed settings with `ExchangeRateApiSettings` class
- ✅ **Environment-Aware:** Supports `appsettings.{Environment}.json` overrides
- ✅ **Flexible:** Individual API enable/disable controls
- ✅ **Injectable:** Uses `IOptions<T>` for dependency injection
- ✅ **Testable:** Easy to mock for unit tests

### HttpClient Configuration
The application uses `IHttpClientFactory` for proper `HttpClient` management with automatic configuration from settings:

```csharp
services.AddHttpClient<Api1JsonAdapter>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<ExchangeRateApiSettings>>().Value;
    client.BaseAddress = new Uri(settings.Api1.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.Api1.TimeoutSeconds);
});
```

**Benefits:**
- Proper socket lifecycle management
- Configurable timeouts per API
- No socket exhaustion
- Built-in retry policies (extensible)

---

## 🐳 Docker Support

### Quick Start with Docker Compose (⭐ Recommended)

The easiest way to run the complete solution with Mock API:

```bash
# Build and start both services (Mock API + Console App)
docker-compose up --build

# Or run in background
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

**What This Does:**
1. ✅ Builds Mock API Docker image
2. ✅ Builds Console App Docker image  
3. ✅ Starts Mock API on port 5298 with health checks
4. ✅ Waits for Mock API to be healthy
5. ✅ Starts Console App (auto-configured to use Mock API)
6. ✅ Shows exchange rate comparison results

### Multi-Service Architecture

```yaml
services:
  mock-api:           # Mock API Server (port 5298)
    └── Provides 3 test endpoints
  
  exchange-rate-comparator:  # Console Application
    └── Queries mock-api and displays best rate
    └── Depends on mock-api being healthy

networks:
  exchange-network:   # Isolated bridge network
```

### Individual Docker Commands

**Build Images Separately:**
```bash
# Console App
docker build -t exchange-rate-comparator:latest .

# Mock API
docker build -t exchange-rate-comparator-mockapi:latest -f Dockerfile.mockapi .
```

**Run Console App Only (with external APIs):**
```bash
docker run --rm \
  -e ExchangeRateApis__Api1__BaseUrl=https://your-api1.example.com/ \
  -e ExchangeRateApis__Api2__BaseUrl=https://your-api2.example.com/ \
  -e ExchangeRateApis__Api3__BaseUrl=https://your-api3.example.com/ \
  exchange-rate-comparator:latest
```

**Run Mock API Only:**
```bash
docker run -d -p 5298:8080 --name mock-api \
  exchange-rate-comparator-mockapi:latest
```

### Docker Features

| Feature | Console App | Mock API | Benefit |
|---------|------------|----------|---------|
| **Multi-stage build** | ✅ | ✅ | Optimal image size (~250MB / ~220MB) |
| **Non-root user** | ✅ | ✅ | Enhanced security (appuser:1000) |
| **Health checks** | ✅ | ✅ | Container monitoring & auto-healing |
| **Resource limits** | ✅ | ✅ | Prevents resource exhaustion |
| **.dockerignore** | ✅ | ✅ | Faster builds, no secrets |
| **Layer caching** | ✅ | ✅ | Incremental builds |
| **Network isolation** | ✅ | ✅ | Secure inter-service communication |

### Configuration in Docker

**Using Mock API (Default):**
```yaml
# docker-compose.yml (already configured)
environment:
  - ExchangeRateApis__Api1__BaseUrl=http://mock-api:8080/
  - ExchangeRateApis__Api2__BaseUrl=http://mock-api:8080/
  - ExchangeRateApis__Api3__BaseUrl=http://mock-api:8080/
```

**Using External APIs:**
Edit `docker-compose.yml`:
```yaml
environment:
  - ExchangeRateApis__Api1__BaseUrl=https://real-api1.example.com/
  - ExchangeRateApis__Api2__BaseUrl=https://real-api2.example.com/
  - ExchangeRateApis__Api3__BaseUrl=https://real-api3.example.com/
  - ExchangeRateApis__Api1__TimeoutSeconds=15
```

### Detailed Documentation

📖 See **[DOCKER.md](DOCKER.md)** for:
- Architecture diagrams
- Security best practices explained
- Troubleshooting guide
- Advanced configuration
- Individual service management
- Resource optimization tips

---

## 🏗️ Design Patterns

- **Strategy Pattern**: Different API adapters implementing `IExchangeRateProvider`
- **Adapter Pattern**: Standardizing different API responses
- **Result Pattern**: `ExchangeResult` for handling success/failure
- **Dependency Injection**: Constructor injection throughout
- **Factory Pattern**: `IHttpClientFactory` for HttpClient instances

## 🎯 SOLID Principles

- **Single Responsibility**: Each class has one reason to change
- **Open/Closed**: Open for extension (add new providers), closed for modification
- **Liskov Substitution**: All providers are interchangeable via `IExchangeRateProvider`
- **Interface Segregation**: Specific interfaces (`IExchangeRateProvider`, `IExchangeRateComparatorService`)
- **Dependency Inversion**: Depends on abstractions, not concretions

## 📈 Performance

- **Parallel Execution**: All APIs are queried simultaneously using `Task.WhenAll()`
- **Timeout Handling**: 10-second default timeout per API (configurable)
- **Async/Await**: Non-blocking operations throughout
- **Minimal Allocations**: Uses C# records and proper disposal patterns

## 📝 Example Output

```
=== Exchange Rate Comparator ===

Comparing exchange rates for 1000.00 USD to EUR...

=== Results ===
Best Rate:        0.8700
Converted Amount: 870.00 EUR
Provider:         API2-XML
Execution Time:   245ms

Press any key to exit...
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License.

## 👨‍💻 Author

**Edwin Roman**
- GitHub: [@Edwinroman30](https://github.com/Edwinroman30)

## 🙏 Acknowledgments

- Built as a technical assessment demonstrating .NET best practices
- Follows Microsoft's recommended patterns and practices
- Inspired by Clean Architecture principles by Robert C. Martin
- Including some of the technique learned throughout the journey