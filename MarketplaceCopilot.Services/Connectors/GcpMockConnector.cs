using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;

namespace MarketplaceCopilot.Services.Connectors;

/// <summary>
/// Mock GCP Marketplace connector — no real GCP Producer Portal access is available in this
/// environment. See AwsMockConnector for the swap-to-real-API rationale.
/// </summary>
public class GcpMockConnector : IMarketplaceConnector
{
    public string Cloud => "GCP";

    public Task<ConnectorConnectResult> ConnectAsync(Tenant tenant, string sellerLabel, CancellationToken ct = default) =>
        Task.FromResult(new ConnectorConnectResult { Success = true });

    public Task<ConnectorSyncResult> FetchProductsAsync(Tenant tenant, MarketplaceConnection connection, CancellationToken ct = default)
    {
        var name = tenant.Name;
        var result = new ConnectorSyncResult
        {
            Products =
            [
                new() { ExternalId = "marketplace-launchpad", Name = $"{name} Marketplace Launchpad", Description = $"Onboards {name}'s SaaS listings onto Google Cloud Marketplace with guided offer and pricing setup.", ListPricePerYear = 90000, BillingModel = "Subscription (Contract)" },
                new() { ExternalId = "producer-portal-connector", Name = $"{name} Producer Portal Connector", Description = "Syncs product and entitlement data between internal systems and the GCP Producer Portal.", ListPricePerYear = 7200, BillingModel = "Usage-Based (PAYG)" },
                new() { ExternalId = "usage-metering-agent", Name = $"{name} Usage Metering Agent", Description = "Automates usage-based billing metering and reporting for GCP Marketplace SaaS listings.", ListPricePerYear = 0, BillingModel = "Free / Nominal" }
            ]
        };
        return Task.FromResult(result);
    }
}
