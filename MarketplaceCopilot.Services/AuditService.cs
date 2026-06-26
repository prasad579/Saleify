using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;

namespace MarketplaceCopilot.Services;

/// <summary>
/// Application-wide audit log. Every meaningful mutation calls <see cref="Log"/>; entries are
/// persisted via the <see cref="DataStore"/> and surfaced read-only on the Global Audit Log screen.
/// </summary>
public class AuditService(DataStore store, IActingUserAccessor actingUser) : IAuditService
{
    public void Log(string category, string action, string details = "",
        string entity = "", string entityId = "", string? user = null)
    {
        store.AppendAudit(new AuditEntry
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
            User = string.IsNullOrWhiteSpace(user) ? actingUser.CurrentUser : user.Trim(),
            Category = category,
            Action = action,
            Details = details ?? "",
            Entity = entity,
            EntityId = entityId
        });
    }

    public void LogDeal(Deal deal, string category, string action, string details = "", string? user = null)
    {
        var label = string.IsNullOrWhiteSpace(deal.Name) ? deal.Id : $"{deal.Id} — {deal.Name}";
        Log(category, action, details, label, deal.Id, user);
    }

    public AuditLogPage Query(string? category, string? entityId, string? search, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 25;
        if (pageSize > 200) pageSize = 200;

        IEnumerable<AuditEntry> q = store.AuditLog;

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
            q = q.Where(e => string.Equals(e.Category, category, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(entityId))
            q = q.Where(e => string.Equals(e.EntityId, entityId, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(e =>
                e.Action.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                e.Details.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                e.Entity.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                e.User.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                e.Category.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        var filtered = q.ToList();
        var paged = filtered.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new AuditLogPage
        {
            Entries = paged,
            Total = filtered.Count,
            Page = page,
            PageSize = pageSize,
            Categories = store.AuditLog.Select(e => e.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList()
        };
    }
}
