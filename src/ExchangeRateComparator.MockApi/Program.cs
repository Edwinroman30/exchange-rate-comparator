using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// API 1: JSON/REST - Returns rate only
app.MapPost("/api/exchange", (Api1Request request) =>
{
    // Simulate different rates for variety
    var rate = request.From switch
    {
        "USD" when request.To == "EUR" => 0.85m + (decimal)(Random.Shared.NextDouble() * 0.05),
        "EUR" when request.To == "USD" => 1.18m + (decimal)(Random.Shared.NextDouble() * 0.05),
        "USD" when request.To == "GBP" => 0.73m + (decimal)(Random.Shared.NextDouble() * 0.05),
        _ => 1.0m
    };

    return Results.Ok(new Api1Response { Rate = rate });
})
.WithName("Api1Exchange")
.WithTags("API1-JSON");

// API 2: XML/SOAP - Returns converted amount directly
app.MapPost("/api/exchange/xml", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var xmlContent = await reader.ReadToEndAsync();
    
    var doc = XDocument.Parse(xmlContent);
    var from = doc.Root?.Element("From")?.Value ?? "USD";
    var to = doc.Root?.Element("To")?.Value ?? "EUR";
    var amount = decimal.Parse(doc.Root?.Element("Amount")?.Value ?? "0");

    // Simulate conversion with slight variation
    var rate = from switch
    {
        "USD" when to == "EUR" => 0.87m + (decimal)(Random.Shared.NextDouble() * 0.03),
        "EUR" when to == "USD" => 1.15m + (decimal)(Random.Shared.NextDouble() * 0.03),
        "USD" when to == "GBP" => 0.75m + (decimal)(Random.Shared.NextDouble() * 0.03),
        _ => 1.0m
    };

    var result = amount * rate;

    var responseXml = new XDocument(
        new XElement("XML",
            new XElement("Result", result)
        )
    );

    context.Response.ContentType = "application/xml";
    await context.Response.WriteAsync(responseXml.ToString());
})
.WithName("Api2Exchange")
.WithTags("API2-XML");

// API 3: Complex JSON - Returns nested structure
app.MapPost("/api/exchange/complex", (Api3Request request) =>
{
    var sourceCurrency = request.Exchange.SourceCurrency;
    var targetCurrency = request.Exchange.TargetCurrency;
    var quantity = request.Exchange.Quantity;

    // Simulate conversion with different rates
    var rate = sourceCurrency switch
    {
        "USD" when targetCurrency == "EUR" => 0.86m + (decimal)(Random.Shared.NextDouble() * 0.04),
        "EUR" when targetCurrency == "USD" => 1.16m + (decimal)(Random.Shared.NextDouble() * 0.04),
        "USD" when targetCurrency == "GBP" => 0.74m + (decimal)(Random.Shared.NextDouble() * 0.04),
        _ => 1.0m
    };

    var total = quantity * rate;

    return Results.Ok(new Api3Response
    {
        StatusCode = 200,
        Message = "Success",
        Data = new Api3Data { Total = total }
    });
})
.WithName("Api3Exchange")
.WithTags("API3-JSON");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck");

app.Run();

// Request/Response models for API 1
record Api1Request(string From, string To, decimal Value);
record Api1Response { public decimal Rate { get; init; } }

// Request/Response models for API 3
record Api3Request(Api3ExchangeInfo Exchange);
record Api3ExchangeInfo(string SourceCurrency, string TargetCurrency, decimal Quantity);
record Api3Response
{
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public Api3Data Data { get; init; } = new();
}
record Api3Data { public decimal Total { get; init; } }

