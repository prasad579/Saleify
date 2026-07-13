using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;

namespace MarketplaceCopilot.Api;

/// <summary>
/// Resolves the current tenant from the caller's auth token (Authorization: Bearer &lt;token&gt;),
/// looked up via UserStore.FindByToken — not from a client-supplied tenant id, since that would let
/// any caller read/write another tenant's data just by setting a header. Falls back to the seeded
/// default tenant when the token is missing/invalid, matching this API's existing lenient (no hard
/// 401) posture elsewhere.
/// </summary>
public class HttpTenantAccessor(IHttpContextAccessor http, UserStore users) : ITenantAccessor
{
    public string TenantId
    {
        get
        {
            var header = http.HttpContext?.Request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(header)) return Tenant.DefaultTenantId;

            var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? header["Bearer ".Length..].Trim()
                : header.Trim();
            if (string.IsNullOrWhiteSpace(token)) return Tenant.DefaultTenantId;

            var user = users.FindByToken(token);
            return string.IsNullOrWhiteSpace(user?.TenantId) ? Tenant.DefaultTenantId : user.TenantId;
        }
    }
}
