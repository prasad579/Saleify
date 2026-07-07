namespace MarketplaceCopilot.Entities;

/// <summary>
/// A request a customer submits from the Customer Portal ("New Engagement Request") — a Private
/// Offer, Free Trial, or POC ask that lands here for the sales team to review, ahead of any
/// internal <see cref="Deal"/> being created for it.
/// </summary>
public class EngagementRequest
{
    public string Id { get; set; } = "";

    // ---- Request Details ----
    public string RequestType { get; set; } = "";
    public string Marketplace { get; set; } = "";
    public List<string> ProductIds { get; set; } = [];

    // ---- Requirements ----
    public string ExpectedStartDate { get; set; } = "";
    public string ExpectedDuration { get; set; } = "";
    public string EstimatedUsers { get; set; } = "";
    public string BusinessNeed { get; set; } = "";
    public string BudgetRange { get; set; } = "";
    public string OtherRequirements { get; set; } = "";

    // ---- Additional information ----
    public string ContactPreference { get; set; } = "";
    public string PreferredTimeToContact { get; set; } = "";

    /// <summary>File names only — the portal captures attachments as UI metadata, not binary uploads.</summary>
    public List<string> AttachmentNames { get; set; } = [];

    /// <summary>Submitted, In Progress, Under Review, Accepted, Declined.</summary>
    public string Status { get; set; } = "Submitted";

    // ---- Requester ----
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CompanyName { get; set; } = "";

    public string CreatedAt { get; set; } = "";
}

/// <summary>Payload the Customer Portal posts to create a new engagement request.</summary>
public class CreateEngagementRequestDto
{
    public string RequestType { get; set; } = "";
    public string Marketplace { get; set; } = "";
    public List<string> ProductIds { get; set; } = [];
    public string ExpectedStartDate { get; set; } = "";
    public string ExpectedDuration { get; set; } = "";
    public string EstimatedUsers { get; set; } = "";
    public string BusinessNeed { get; set; } = "";
    public string BudgetRange { get; set; } = "";
    public string OtherRequirements { get; set; } = "";
    public string ContactPreference { get; set; } = "";
    public string PreferredTimeToContact { get; set; } = "";
    public List<string> AttachmentNames { get; set; } = [];
}
