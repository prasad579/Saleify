namespace MarketplaceCopilot.Services.Contracts;

/// <summary>
/// Resolves the tenant for the current request, derived server-side from the caller's auth token
/// (via UserStore.FindByToken) rather than trusted from a client-supplied value — unlike
/// IActingUserAccessor (audit display only), a wrong tenant here would leak another org's data.
/// Falls back to Tenant.DefaultTenantId when the token is missing/invalid, matching this API's
/// existing lenient (no hard 401) posture.
/// </summary>
public interface ITenantAccessor
{
    string TenantId { get; }
}
