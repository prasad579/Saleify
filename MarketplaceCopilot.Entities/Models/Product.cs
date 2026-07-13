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

    /// <summary>Owning tenant — every product belongs to exactly one tenant's catalog.</summary>
    public string TenantId { get; set; } = "";
    /// <summary>Which of the tenant's marketplace connections synced this product ("AWS"/"Azure"/"GCP").</summary>
    public string Cloud { get; set; } = "";
    /// <summary>Stable id from the connector's source listing — used to upsert on re-sync without changing Id.</summary>
    public string ExternalId { get; set; } = "";
    /// <summary>
    /// True once a re-sync no longer returns this product (or its connection was disconnected). Hidden
    /// from new product selection but still resolvable, so existing deals that already picked it keep working.
    /// </summary>
    public bool Discontinued { get; set; }
}
