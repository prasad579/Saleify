namespace MarketplaceCopilot.Entities;

/// <summary>
/// A single entry in the application-wide audit log: who did what, when, to which entity,
/// with enough detail to reconstruct the change. Written for every meaningful mutation across
/// the app (engagements, pricing, approvals, and every settings change) and surfaced read-only
/// on the Global Audit Log screen.
/// </summary>
public class AuditEntry
{
    public string Id { get; set; } = "";
    public string Timestamp { get; set; } = "";
    /// <summary>The acting user (resolved from the X-Acting-User header, falling back to "System").</summary>
    public string User { get; set; } = "";
    /// <summary>Functional area — Engagement, Products, Pricing, Meeting Notes, Approvals, Settings, etc.</summary>
    public string Category { get; set; } = "";
    /// <summary>What happened, e.g. "Engagement created", "Pricing updated", "Approval rules saved".</summary>
    public string Action { get; set; } = "";
    /// <summary>Human-readable detail behind the action.</summary>
    public string Details { get; set; } = "";
    /// <summary>Display label of the affected entity, e.g. "DL-1001 — Infosys Ltd." or "Approval Rules".</summary>
    public string Entity { get; set; } = "";
    /// <summary>Stable id of the affected entity for filtering / linking (e.g. "DL-1001", "approval-rules").</summary>
    public string EntityId { get; set; } = "";
}

/// <summary>A page of audit entries plus the metadata the UI needs to render filters and pagination.</summary>
public class AuditLogPage
{
    public List<AuditEntry> Entries { get; set; } = [];
    public int Total { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    /// <summary>Distinct categories present in the full log, for the filter dropdown.</summary>
    public List<string> Categories { get; set; } = [];
}
