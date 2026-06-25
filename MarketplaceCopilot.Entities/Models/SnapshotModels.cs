namespace MarketplaceCopilot.Entities;

/// <summary>
/// Describes which engagements an Engagement Snapshot should cover.
/// Scope drives how the engagement set is resolved:
///   engagement → a single deal (DealId)
///   event      → all deals tagged to a campaign/event (EventId)
///   filtered   → deals matching the supplied filters (mirrors the Engagements list filters)
///   dashboard  → all accessible engagements (leadership-wide summary)
/// </summary>
public class SnapshotRequest
{
    public string Scope { get; set; } = "dashboard";
    public string? DealId { get; set; }
    public string? EventId { get; set; }

    // Filters (used by scope == "filtered"; mirror the Engagements list query params).
    public string? Owner { get; set; }
    public string? Stage { get; set; }
    public string? Status { get; set; }
    public string? Tag { get; set; }
    public string? Marketplace { get; set; }
    public string? EngagementType { get; set; }
    public string? Search { get; set; }
    /// <summary>Exclude closed/abandoned engagements (the "open only" scope from the list view).</summary>
    public bool OpenOnly { get; set; }
}

/// <summary>A snapshot request plus the recipients/subject needed to email it.</summary>
public class EmailSummaryRequest : SnapshotRequest
{
    public List<string> To { get; set; } = [];
    public List<string> Cc { get; set; } = [];
    public string? Subject { get; set; }
}

/// <summary>The full Engagement Snapshot, structured into the five report sections.</summary>
public class EngagementSnapshot
{
    public string Title { get; set; } = "";
    public string Scope { get; set; } = "";
    /// <summary>
    /// The engagement type when the snapshot covers a single engagement (scope == "engagement"),
    /// otherwise empty. Lets the UI render type-specific layouts (e.g. the compact email-style
    /// layout used for a Workshop summary).
    /// </summary>
    public string EngagementType { get; set; } = "";
    public string GeneratedAt { get; set; } = "";
    /// <summary>Default email subject: "&lt;Title&gt; - Engagement Summary".</summary>
    public string SuggestedSubject { get; set; } = "";

    /// <summary>Section 1 — shown only when an event/campaign tag applies.</summary>
    public EventInfoSection? Event { get; set; }
    public EngagementSummarySection Summary { get; set; } = new();
    public PipelineSummarySection Pipeline { get; set; } = new();
    public List<AttentionRow> Attention { get; set; } = [];
    public List<PrivateOfferRow> PrivateOffers { get; set; } = [];
    /// <summary>Effective display settings (which sections/fields to show, custom labels) for this snapshot.</summary>
    public SnapshotSettings Settings { get; set; } = new();
}

public class EventInfoSection
{
    public string Name { get; set; } = "";
    public string StartDate { get; set; } = "";
    public string EndDate { get; set; } = "";
    public string Status { get; set; } = "";
}

public class EngagementSummarySection
{
    public int Total { get; set; }
    public List<SnapshotCount> ByType { get; set; } = [];
}

public class SnapshotCount
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
}

public class PipelineSummarySection
{
    public decimal ExpectedPipelineValue { get; set; }
    public string ExpectedPipelineDisplay { get; set; } = "";
    public int ActivePrivateOffers { get; set; }
}

public class AttentionRow
{
    public string Customer { get; set; } = "";
    public string EngagementType { get; set; } = "";
    public string Owner { get; set; } = "";
    public string Status { get; set; } = "";
    public string NextActionDate { get; set; } = "";
    public string DealId { get; set; } = "";
    public string Link { get; set; } = "";
}

public class PrivateOfferRow
{
    public string Customer { get; set; } = "";
    public string Marketplace { get; set; } = "";
    public string OfferValue { get; set; } = "";
    public string Status { get; set; } = "";
    public string ExpectedCloseDate { get; set; } = "";
    public string DealId { get; set; } = "";
    public string Link { get; set; } = "";
}

/// <summary>Counters for the Home "Engagement Insights" dashboard card.</summary>
public class DashboardInsights
{
    public int ActiveEvents { get; set; }
    public int ActiveEngagements { get; set; }
    public int PendingFollowUps { get; set; }
    public int PendingApprovals { get; set; }
}

public class EmailSummaryResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string Subject { get; set; } = "";
    public List<string> To { get; set; } = [];
    public string BodyHtml { get; set; } = "";
    /// <summary>True when an SMTP server was configured and the message was actually sent.</summary>
    public bool Delivered { get; set; }
}
