using MarketplaceCopilot.Entities;

namespace MarketplaceCopilot.Services.Contracts;

/// <summary>
/// Records application-wide audit entries (who / what / when / details) for the Global Audit Log.
/// The acting user is resolved from the request when not supplied explicitly.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Append an audit entry. <paramref name="user"/> is optional — when null the acting user is
    /// resolved from the current request (X-Acting-User header), falling back to "System".
    /// </summary>
    void Log(string category, string action, string details = "",
        string entity = "", string entityId = "", string? user = null);

    /// <summary>Convenience overload for deal-scoped changes — fills entity/entityId from the deal.</summary>
    void LogDeal(Deal deal, string category, string action, string details = "", string? user = null);

    /// <summary>Return a filtered, paged view of the audit log.</summary>
    AuditLogPage Query(string? category, string? entityId, string? search, int page, int pageSize);
}
