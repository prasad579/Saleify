using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;

namespace MarketplaceCopilot.Services.Connectors;

/// <summary>
/// Mock Azure Marketplace connector — no real Azure Partner Center app registration is available in
/// this environment. See AwsMockConnector for the swap-to-real-API rationale.
/// </summary>
public class AzureMockConnector : IMarketplaceConnector
{
    public string Cloud => "Azure";

    public Task<ConnectorConnectResult> ConnectAsync(Tenant tenant, string sellerLabel, CancellationToken ct = default) =>
        Task.FromResult(new ConnectorConnectResult { Success = true });

    public Task<ConnectorSyncResult> FetchProductsAsync(Tenant tenant, MarketplaceConnection connection, CancellationToken ct = default)
    {
        var name = tenant.Name;
        var result = new ConnectorSyncResult
        {
            Products =
            [
                new() { ExternalId = "platform", Name = $"{name} Cloud Marketplace Platform", Description = $"Manages the full lifecycle of {name}'s transactable offers in Azure Marketplace — offer management, pricing recommendations, subscription lifecycle.", ListPricePerYear = 7500, BillingModel = "Subscription (Contract)" },
                new() { ExternalId = "startup-platform", Name = $"{name} Cloud Marketplace Platform for Startups", Description = "Startup edition of the Cloud Marketplace Platform for Azure ISVs under $10M revenue.", ListPricePerYear = 5000, BillingModel = "Subscription (Contract) — Startup tier" },
                new() { ExternalId = "saas-contract", Name = $"{name} SaaS Contract", Description = "Private-offer / annual-commitment edition of the Cloud Marketplace Platform.", ListPricePerYear = 7500, BillingModel = "Subscription (Contract) — Private Offer" },
                new() { ExternalId = "onboarding-agent", Name = $"{name} AI Onboarding Accelerator", Description = "Agentic AI that accelerates Azure Marketplace onboarding — validates offer parameters, automates compliance checks.", ListPricePerYear = 0, BillingModel = "Free / Nominal" }
            ]
        };
        return Task.FromResult(result);
    }
}
