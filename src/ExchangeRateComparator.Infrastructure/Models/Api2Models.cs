using System.Xml.Serialization;

namespace ExchangeRateComparator.Infrastructure.Models;

/// <summary>
/// Request model for API 2 (XML/SOAP)
/// </summary>
[XmlRoot("XML")]
public class Api2Request
{
    [XmlElement("From")]
    public string From { get; set; } = string.Empty;

    [XmlElement("To")]
    public string To { get; set; } = string.Empty;

    [XmlElement("Amount")]
    public decimal Amount { get; set; }
}

/// <summary>
/// Response model for API 2 (XML/SOAP)
/// </summary>
[XmlRoot("XML")]
public class Api2Response
{
    [XmlElement("Result")]
    public decimal Result { get; set; }
}
