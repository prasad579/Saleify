namespace MarketplaceCopilot.Entities;

/// <summary>
/// A record of an engagement that was submitted ("pushed") to a destination such as SaaSify or a
/// cloud marketplace. Captures the exact JSON payload that was pushed and, optionally, the response
/// captured back from the destination. Surfaced on the Offer Requests page.
/// </summary>
public class OfferRequest
{
    public string Id { get; set; } = "";
    public string DealId { get; set; } = "";
    public string EngagementName { get; set; } = "";
    public string Customer { get; set; } = "";
    public string EngagementType { get; set; } = "";
    public string Marketplace { get; set; } = "";
    /// <summary>Product names included in the offer (for display + filtering).</summary>
    public List<string> Products { get; set; } = [];
    /// <summary>Where the engagement was pushed (e.g. "SaaSify", "AWS Marketplace").</summary>
    public string Destination { get; set; } = "SaaSify";
    /// <summary>Submission status mirrored from the engagement (In Review, Completed, Lead, Published…).</summary>
    public string Status { get; set; } = "Submitted";
    public decimal Value { get; set; }
    public string SubmittedAt { get; set; } = "";
    public string SubmittedBy { get; set; } = "";
    /// <summary>The full JSON payload pushed to the destination (pretty-printed).</summary>
    public string RequestJson { get; set; } = "";

    /// <summary>True when the engagement was edited after this offer request was last pushed.</summary>
    public bool ChangedSinceSubmission { get; set; }
    /// <summary>What changed on the engagement since the last push (shown on the offer request).</summary>
    public string LastChangeSummary { get; set; } = "";

    // ---- Response captured back from the destination ----
    public bool ResponseReceived { get; set; }
    /// <summary>Accepted, Rejected, Changes Requested, Pending.</summary>
    public string ResponseStatus { get; set; } = "";
    /// <summary>External reference id returned by the destination (e.g. a marketplace offer id).</summary>
    public string ResponseReference { get; set; } = "";
    public string ResponseNotes { get; set; } = "";
    public string ResponseJson { get; set; } = "";
    public string ResponseAt { get; set; } = "";
    public string ResponseBy { get; set; } = "";
}

/// <summary>Payload for capturing the destination's response to an offer request.</summary>
public class CaptureResponseRequest
{
    public string Status { get; set; } = "";
    public string Reference { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Json { get; set; } = "";
    public string User { get; set; } = "";
}
