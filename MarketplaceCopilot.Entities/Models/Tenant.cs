namespace MarketplaceCopilot.Entities;

/// <summary>
/// A separate customer org using Marketplace Copilot (e.g. "SaaSify") — owns its own users, deals,
/// and product catalog. Products are populated per cloud by connecting a marketplace seller account
/// through a <see cref="MarketplaceConnection"/>.
/// </summary>
public class Tenant
{
    /// <summary>Fallback tenant id used when one can't be resolved for the current request.</summary>
    public const string DefaultTenantId = "TEN-1";

    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public List<MarketplaceConnection> Connections { get; set; } = [];
}

/// <summary>One tenant's connection to a single cloud marketplace (AWS, Azure, or GCP).</summary>
public class MarketplaceConnection
{
    public string Cloud { get; set; } = "";
    /// <summary>Not Connected, Connected, Syncing, or Error.</summary>
    public string Status { get; set; } = "Not Connected";
    /// <summary>Display-only mock seller/account identifier entered when connecting.</summary>
    public string SellerLabel { get; set; } = "";
    public string ConnectedAt { get; set; } = "";
    public string LastSyncedAt { get; set; } = "";
    public int ProductCount { get; set; }
    public string LastError { get; set; } = "";
}
