using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;

namespace MarketplaceCopilot.Services.Connectors;

/// <summary>
/// Mock AWS Marketplace connector — no real AWS Marketplace Catalog API credentials are available in
/// this environment. Generates a plausible product set for any tenant so the connect/sync flow is
/// fully demoable; a real implementation (calling the Catalog API with the tenant's seller IAM role)
/// can replace this without changing the IMarketplaceConnector contract.
/// </summary>
public class AwsMockConnector : IMarketplaceConnector
{
    public string Cloud => "AWS";

    public Task<ConnectorConnectResult> ConnectAsync(Tenant tenant, string sellerLabel, CancellationToken ct = default) =>
        Task.FromResult(new ConnectorConnectResult { Success = true });

    public Task<ConnectorSyncResult> FetchProductsAsync(Tenant tenant, MarketplaceConnection connection, CancellationToken ct = default)
    {
        var name = tenant.Name;
        var result = new ConnectorSyncResult
        {
            Products =
            [
                new() { ExternalId = "platform", Name = $"{name} Cloud GTM Platform", Description = $"Single integrated platform to list and manage {name}'s SaaS offers across AWS Marketplace — offer management, subscription lifecycle, and analytics.", ListPricePerYear = 120000, BillingModel = "Subscription (Contract)" },
                new() { ExternalId = "ace-connector-salesforce", Name = $"{name} AWS ACE Connector for Salesforce", Description = "No-code, bi-directional sync between Salesforce CRM and the AWS Partner Network's ACE co-sell pipeline.", ListPricePerYear = 8988, BillingModel = "Usage-Based (PAYG)" },
                new() { ExternalId = "ace-connector-hubspot", Name = $"{name} AWS ACE Connector for HubSpot", Description = "No-code, bi-directional sync between HubSpot CRM and the AWS Partner Network's ACE co-sell pipeline.", ListPricePerYear = 6000, BillingModel = "Usage-Based (PAYG) — 30-day free trial" },
                new() { ExternalId = "partner-migration-agent", Name = $"{name} Partner Central Migration Agent", Description = "AI-powered self-service agent for AWS Partner Central migration — assesses setup, flags compliance risks, automates IAM setup.", ListPricePerYear = 0, BillingModel = "Free / Nominal" }
            ]
        };
        return Task.FromResult(result);
    }
}
