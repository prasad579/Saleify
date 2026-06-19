using System.Text.Json.Serialization;

namespace MarketplaceCopilot.Entities;

public class CreateDealRequest
{
    public string Name { get; set; } = "";
    public string Customer { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string Location { get; set; } = "";
    public string Industry { get; set; } = "";
    public string Marketplace { get; set; } = "";
    public string DealType { get; set; } = "New Deal";
    public decimal ExpectedValue { get; set; }
    public string ExpectedCloseDate { get; set; } = "";
    public string Description { get; set; } = "";
}

public class CreateDealResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Deal? Deal { get; set; }
}

public class LookupData
{
    public List<string> Countries { get; set; } = [];
    public List<string> Industries { get; set; } = [];
    public List<string> DealTypes { get; set; } = [];
    public List<string> Marketplaces { get; set; } = [];
}
