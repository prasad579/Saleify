namespace MarketplaceCopilot.Entities;

public class Deal
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Customer { get; set; } = "";
    public string ContactName { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Location { get; set; } = "";
    public string Industry { get; set; } = "";
    public string Marketplace { get; set; } = "";
    public string DealType { get; set; } = "New Deal";
    public string EngagementType { get; set; } = "";
    public bool QuickCapture { get; set; }
    public string CampaignEventId { get; set; } = "";
    public string CampaignEventName { get; set; } = "";
    public string BillingAccountId { get; set; } = "";
    /// <summary>Per-marketplace billing account IDs, keyed by marketplace (e.g. "AWS", "Azure", "GCP").</summary>
    public Dictionary<string, string> BillingAccountIds { get; set; } = new();
    public decimal ExpectedValue { get; set; }
    public string ExpectedCloseDate { get; set; } = "";
    public string Description { get; set; } = "";
    public string Stage { get; set; } = "Draft";
    public int StepNumber { get; set; } = 1;
    public int TotalSteps { get; set; } = 5;
    public string MarketplaceStatus { get; set; } = "Draft";
    public string Owner { get; set; } = "Srinivas K";
    public string LastUpdated { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    /// <summary>Soft-hidden from active views; restorable. Tasks/reminders are preserved while archived.</summary>
    public bool Archived { get; set; }
    public string ArchivedAt { get; set; } = "";
    public List<string> ProductIds { get; set; } = [];
    public PricingConfig? Pricing { get; set; }
    public MeetingNotesData? MeetingNotes { get; set; }
    public List<DealChangeEntry> ChangeHistory { get; set; } = [];
    public ApprovalData? Approvals { get; set; }
}

public class PricingConfig
{
    public string OfferType { get; set; } = "Direct Private Offer";
    public string ContractStart { get; set; } = "";
    public string ContractEnd { get; set; } = "";
    public string DurationType { get; set; } = "years";
    public int DurationValue { get; set; } = 3;
    public int TrialDays { get; set; } = 14;
    public int DurationDays { get; set; }
    public int DurationMonths { get; set; } = 36;
    public string PricingMethod { get; set; } = "Discount Based";
    public decimal PublicPricePerYear { get; set; } = 120000;
    public string DiscountModel { get; set; } = "Same discount for entire contract";
    public decimal DiscountPercent { get; set; } = 15;
    public List<decimal> YearlyDiscountPercents { get; set; } = [];
    public decimal AbsoluteContractPrice { get; set; }
    public bool ProRateEnabled { get; set; }
    public bool FlexiblePaymentsEnabled { get; set; }
    public int InstallmentCount { get; set; } = 1;
    public decimal MarketplaceFeePercent { get; set; } = 5;
    public decimal PublicContractValue { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal NetPriceBeforeFees { get; set; }
    public decimal MarketplaceFee { get; set; }
    public decimal NetContractValue { get; set; }
    public decimal TotalPayable { get; set; }
    public List<YearlyPricingRow> YearlyBreakdown { get; set; } = [];
    public List<InstallmentRow> InstallmentSchedule { get; set; } = [];
}

public class MeetingNotesData
{
    public string RawNotes { get; set; } = "";
    public AiExtractedSummary? Extracted { get; set; }
    public List<MeetingNoteSession> Sessions { get; set; } = [];
    public List<ActionItem> ActionItems { get; set; } = [];
    public List<DealReminder> Reminders { get; set; } = [];
}

public class MeetingNoteSession
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string RawNotes { get; set; } = "";
    public AiExtractedSummary? Extracted { get; set; }
    public string CreatedAt { get; set; } = "";
}

public class DealReminder
{
    public string Id { get; set; } = "";
    public string Reminder { get; set; } = "";
    /// <summary>The date to be reminded on (yyyy-MM-dd). Drives the due / overdue status.</summary>
    public string DateTime { get; set; } = "";
    public string Type { get; set; } = "Follow-up";
    /// <summary>When the reminder was created (yyyy-MM-dd). Set on first save.</summary>
    public string CreatedAt { get; set; } = "";
    /// <summary>Optional link to a meeting session.</summary>
    public string SessionId { get; set; } = "";
}

public class DealChangeEntry
{
    public string Id { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public string Category { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Details { get; set; } = "";
    public string ChangedBy { get; set; } = "";
}

public class AiExtractedSummary
{
    public List<SummaryFieldRow> StandardFields { get; set; } = [];
    public List<SummaryFieldRow> DynamicFields { get; set; } = [];
}

public class SummaryFieldRow
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
}

public class ActionItem
{
    public string Id { get; set; } = "";
    public string Task { get; set; } = "";
    public string Owner { get; set; } = "";
    /// <summary>ISO date yyyy-MM-dd when known; empty if user must set.</summary>
    public string DueDate { get; set; } = "";
    /// <summary>Relative hint parsed from notes, e.g. "Next Friday (from notes)".</summary>
    public string DueDateHint { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string Source { get; set; } = "ai";
    /// <summary>Optional link to a meeting session.</summary>
    public string SessionId { get; set; } = "";
}

public class ApprovalData
{
    public string Version { get; set; } = "V1.0";
    public string LastUpdatedAt { get; set; } = "";
    public string LastUpdatedBy { get; set; } = "";
    public string PricingFingerprint { get; set; } = "";
    public bool ChangesPendingReapproval { get; set; }
    public string? ChangeSummary { get; set; }
    public bool DocumentsLocked { get; set; }
    public bool AllowPostApprovalEdits { get; set; } = true;
    public string EulaStatus { get; set; } = "Draft";
    public string PackageSummaryId { get; set; } = "doc-package";
    public List<ApprovalRuleMatch> RuleMatches { get; set; } = [];
    public List<ApprovalStep> Steps { get; set; } = [];
    public List<ApprovalDocument> Documents { get; set; } = [];
    public List<ApprovalAuditEntry> AuditLog { get; set; } = [];
}

public class ApprovalRuleMatch
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Assignee { get; set; } = "";
    public string Reason { get; set; } = "";
    public bool Matched { get; set; }
}

public class ApprovalStep
{
    public string Id { get; set; } = "";
    public int Order { get; set; }
    public string Title { get; set; } = "";
    public string Assignee { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string RuleReason { get; set; } = "";
    public string? CompletedAt { get; set; }
    public string? Comment { get; set; }
    public List<ApprovalComment> Comments { get; set; } = [];
}

public class ApprovalComment
{
    public string Id { get; set; } = "";
    public string Author { get; set; } = "";
    public string Text { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public string Type { get; set; } = "comment";
}

public class ApprovalDocument
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string DocumentType { get; set; } = "";
    public string Version { get; set; } = "";
    public string GeneratedAt { get; set; } = "";
    public string FileName { get; set; } = "";
    public bool Stale { get; set; }
    public bool Locked { get; set; }
    /// <summary>100 = complete; lower for partial EULA draft.</summary>
    public int FillPercent { get; set; } = 100;
    public string HtmlContent { get; set; } = "";
}

public class ApprovalAuditEntry
{
    public string Id { get; set; } = "";
    public string Timestamp { get; set; } = "";
    public string Category { get; set; } = "Approval";
    public string User { get; set; } = "";
    public string Action { get; set; } = "";
    public string Details { get; set; } = "";
    public string StepTitle { get; set; } = "";
    public string DocumentName { get; set; } = "";
}
