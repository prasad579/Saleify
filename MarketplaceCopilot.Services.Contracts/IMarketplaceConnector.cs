using MarketplaceCopilot.Entities;

namespace MarketplaceCopilot.Services.Contracts;

/// <summary>
/// Syncs a tenant's product catalog from one cloud marketplace (AWS/Azure/GCP). One implementation
/// per cloud; today's implementations are mocks (no real seller credentials available) but the
/// interface is shaped so a real API-backed implementation (AWS Marketplace Catalog API, Azure
/// Partner Center, GCP Producer Portal) is a drop-in replacement — connect (credential/auth
/// handshake) is split from fetch (data pull), and results carry partial-success/error info.
/// </summary>
public interface IMarketplaceConnector
{
    /// <summary>"AWS" | "Azure" | "GCP".</summary>
    string Cloud { get; }

    Task<ConnectorConnectResult> ConnectAsync(Tenant tenant, string sellerLabel, CancellationToken ct = default);

    Task<ConnectorSyncResult> FetchProductsAsync(Tenant tenant, MarketplaceConnection connection, CancellationToken ct = default);
}

public class ConnectorConnectResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public class ConnectorSyncResult
{
    public List<ConnectorProduct> Products { get; set; } = [];
    public bool IsPartial { get; set; }
    public string? Error { get; set; }
}

/// <summary>A single product as returned by a connector, before it's mapped onto the internal Product entity.</summary>
public class ConnectorProduct
{
    /// <summary>Stable id from the source marketplace listing (SKU/slug) — must not change between syncs.</summary>
    public string ExternalId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal ListPricePerYear { get; set; }
    public string BillingModel { get; set; } = "Subscription";
}
