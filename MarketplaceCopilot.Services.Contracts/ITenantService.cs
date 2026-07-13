using MarketplaceCopilot.Entities;

namespace MarketplaceCopilot.Services.Contracts;

/// <summary>Manages tenants and their marketplace connections (connect / sync / disconnect a cloud).</summary>
public interface ITenantService
{
    Tenant? GetById(string id);

    /// <summary>Case-insensitive match on company name; creates a new tenant (no connections) if none matches.</summary>
    Tenant GetOrCreateByCompanyName(string name);

    Task<Tenant> ConnectAsync(string tenantId, string cloud, string sellerLabel);

    Task<Tenant> SyncAsync(string tenantId, string cloud);

    Tenant Disconnect(string tenantId, string cloud);
}
