using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;

namespace MarketplaceCopilot.Services;

public class TenantService(DataStore store, IEnumerable<IMarketplaceConnector> connectors, IAuditService audit) : ITenantService
{
    // Products had zero writers before this service — guard the new read-modify-write mutation
    // paths (Connect/Sync/Disconnect) against concurrent requests racing the same tenant+cloud.
    private readonly SemaphoreSlim _lock = new(1, 1);

    public Tenant? GetById(string id) => store.Tenants.FirstOrDefault(t => t.Id == id);

    public Tenant GetOrCreateByCompanyName(string name)
    {
        var trimmed = name.Trim();
        var existing = store.Tenants.FirstOrDefault(t => t.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return existing;

        var tenant = new Tenant
        {
            Id = store.NextTenantId(),
            Name = trimmed,
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd")
        };
        store.Tenants.Insert(0, tenant);
        store.SaveTenants();
        audit.Log("Tenant", "Tenant created", $"{tenant.Name} ({tenant.Id}) created.", entity: "Tenant", entityId: tenant.Id);
        return tenant;
    }

    public async Task<Tenant> ConnectAsync(string tenantId, string cloud, string sellerLabel)
    {
        var tenant = GetById(tenantId) ?? throw new InvalidOperationException($"Tenant {tenantId} not found.");
        var connector = ResolveConnector(cloud);
        var connectResult = await connector.ConnectAsync(tenant, sellerLabel);

        await _lock.WaitAsync();
        try
        {
            var connection = GetOrAddConnection(tenant, connector.Cloud);
            if (!connectResult.Success)
            {
                connection.Status = "Error";
                connection.LastError = connectResult.Error ?? "Connection failed.";
                store.SaveTenants();
                audit.Log("Tenant", "Marketplace connect failed", $"{tenant.Name} — {connector.Cloud}: {connection.LastError}", entity: "Tenant", entityId: tenant.Id);
                return tenant;
            }

            connection.Status = "Connected";
            connection.SellerLabel = sellerLabel;
            connection.ConnectedAt = Now();
            connection.LastError = "";
            await SyncInternal(tenant, connection, connector);
            audit.Log("Tenant", "Marketplace connected", $"{tenant.Name} connected {connector.Cloud} (seller: {sellerLabel}).", entity: "Tenant", entityId: tenant.Id);
            return tenant;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Tenant> SyncAsync(string tenantId, string cloud)
    {
        var tenant = GetById(tenantId) ?? throw new InvalidOperationException($"Tenant {tenantId} not found.");
        var connector = ResolveConnector(cloud);
        var connection = tenant.Connections.FirstOrDefault(c => c.Cloud.Equals(connector.Cloud, StringComparison.OrdinalIgnoreCase));
        if (connection is null || connection.Status == "Not Connected")
            throw new InvalidOperationException($"{connector.Cloud} is not connected for {tenant.Name}.");

        await _lock.WaitAsync();
        try
        {
            await SyncInternal(tenant, connection, connector);
            audit.Log("Tenant", "Marketplace synced", $"{tenant.Name} — {connector.Cloud}: {connection.ProductCount} product(s).", entity: "Tenant", entityId: tenant.Id);
            return tenant;
        }
        finally
        {
            _lock.Release();
        }
    }

    public Tenant Disconnect(string tenantId, string cloud)
    {
        var tenant = GetById(tenantId) ?? throw new InvalidOperationException($"Tenant {tenantId} not found.");
        var connector = ResolveConnector(cloud);

        _lock.Wait();
        try
        {
            var connection = tenant.Connections.FirstOrDefault(c => c.Cloud.Equals(connector.Cloud, StringComparison.OrdinalIgnoreCase));
            if (connection is null) return tenant;

            // Mark discontinued rather than delete — an existing deal may already reference one of
            // these products, and it must keep resolving via GetById after disconnect.
            foreach (var p in store.Products.Where(p => p.TenantId == tenantId && p.Cloud == connector.Cloud))
                p.Discontinued = true;

            connection.Status = "Not Connected";
            connection.SellerLabel = "";
            connection.ProductCount = 0;
            connection.LastError = "";
            store.SaveProducts();
            store.SaveTenants();
            audit.Log("Tenant", "Marketplace disconnected", $"{tenant.Name} disconnected {connector.Cloud}.", entity: "Tenant", entityId: tenant.Id);
            return tenant;
        }
        finally
        {
            _lock.Release();
        }
    }

    // ---------------- Internals ----------------

    private async Task SyncInternal(Tenant tenant, MarketplaceConnection connection, IMarketplaceConnector connector)
    {
        connection.Status = "Syncing";
        var result = await connector.FetchProductsAsync(tenant, connection);

        var existing = store.Products.Where(p => p.TenantId == tenant.Id && p.Cloud == connector.Cloud).ToList();
        var fetchedIds = new HashSet<string>();

        foreach (var cp in result.Products)
        {
            var id = ComposeProductId(tenant.Id, connector.Cloud, cp.ExternalId);
            fetchedIds.Add(id);
            var product = existing.FirstOrDefault(p => p.Id == id);
            if (product is null)
            {
                product = new Product { Id = id, TenantId = tenant.Id, Cloud = connector.Cloud, ExternalId = cp.ExternalId };
                store.Products.Add(product);
            }
            product.Name = cp.Name;
            product.Description = cp.Description;
            product.Marketplaces = [connector.Cloud];
            product.ListPricePerYear = cp.ListPricePerYear;
            product.BillingModel = cp.BillingModel;
            product.Discontinued = false;
        }

        // Anything previously synced from this tenant+cloud that the connector no longer returns —
        // hide it from new selection, but don't delete (an existing deal may already reference it).
        foreach (var p in existing.Where(p => !fetchedIds.Contains(p.Id)))
            p.Discontinued = true;

        connection.Status = string.IsNullOrWhiteSpace(result.Error) ? "Connected" : "Error";
        connection.LastError = result.Error ?? "";
        connection.LastSyncedAt = Now();
        connection.ProductCount = store.Products.Count(p => p.TenantId == tenant.Id && p.Cloud == connector.Cloud && !p.Discontinued);

        store.SaveProducts();
        store.SaveTenants();
    }

    private static MarketplaceConnection GetOrAddConnection(Tenant tenant, string cloud)
    {
        var connection = tenant.Connections.FirstOrDefault(c => c.Cloud.Equals(cloud, StringComparison.OrdinalIgnoreCase));
        if (connection is not null) return connection;

        connection = new MarketplaceConnection { Cloud = cloud };
        tenant.Connections.Add(connection);
        return connection;
    }

    private IMarketplaceConnector ResolveConnector(string cloud) =>
        connectors.FirstOrDefault(c => c.Cloud.Equals(cloud, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No connector registered for cloud '{cloud}'.");

    private static string ComposeProductId(string tenantId, string cloud, string externalId) =>
        $"{tenantId}-{cloud}-{externalId}".ToLowerInvariant();

    private static string Now() => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
}
