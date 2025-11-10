# Exchange Rate API Comparison Service

## Project Overview
Build a currency exchange rate comparison service that queries multiple APIs simultaneously, handles failures gracefully, and returns the best conversion rate in the shortest time possible.

## Core Objectives
- Query multiple exchange rate APIs with different signatures
- Return the highest conversion amount
- Minimize response time using async/await and parallel execution
- Handle API failures gracefully
- No UI or SQL database required
- Console application or Class Library with unit tests

---


## Functional Requirements

### Input
```csharp
public class ExchangeRequest
{
    public string SourceCurrency { get; set; }  // e.g., "USD"
    public string TargetCurrency { get; set; }  // e.g., "EUR"
    public decimal Amount { get; set; }         // e.g., 1000.00M
}
```

### Output
```csharp
public class ExchangeResponse
{
    public decimal BestRate { get; set; }
    public decimal ConvertedAmount { get; set; }
    public string Provider { get; set; }
    public long ExecutionTimeMs { get; set; }
}
```

### API Specifications

#### API 1 (JSON/REST)
- **Input:** `{ "from": "USD", "to": "EUR", "value": 1000.00 }`
- **Output:** `{ "rate": 0.85 }`
- **Calculation:** `convertedAmount = value * rate`

#### API 2 (XML/SOAP)
- **Input:** `<XML><From>USD</From><To>EUR</To><Amount>1000.00</Amount></XML>`
- **Output:** `<XML><Result>850.00</Result></XML>`
- **Calculation:** `convertedAmount = Result`

#### API 3 (JSON/REST)
- **Input:** `{ "exchange": { "sourceCurrency": "USD", "targetCurrency": "EUR", "quantity": 1000.00 }}`
- **Output:** `{ "statusCode": 200, "message": "Success", "data": { "total": 850.00 }}`
- **Calculation:** `convertedAmount = data.total`

---

## Considerations: 

- The code must be versioned in a personal public repository (e.g., GitHub). 
- The project must be fully testable by the evaluators. 
- Using containerization technology will be considered a plus. 
- Assistance from other individuals is not allowed; the project must be completed 
independently. 
- No additional information will be provided beyond what is described in the attached 
document. 
- You are expected to analyze, design, and develop a solution that best meets the stated requirements. 

