namespace MarketplaceCopilot.Entities;

public class Product
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Family { get; set; } = "SaaSify";
    public string Description { get; set; } = "";
    public List<string> Marketplaces { get; set; } = [];
    public decimal ListPricePerYear { get; set; }
    public string BillingModel { get; set; } = "Subscription";
}
