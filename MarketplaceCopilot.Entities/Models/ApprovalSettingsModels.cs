namespace MarketplaceCopilot.Entities;

/// <summary>
/// Customizable approval rules that decide which reviews an engagement requires.
/// Editable from Settings → Approval Rules so the discount / duration thresholds, the
/// reviewers, and which engagement types each rule applies to can change without a code
/// change (e.g. "Private Offer discount &gt; 15% requires Finance Review").
/// </summary>
public class ApprovalRulesSettings
{
    public List<ApprovalRuleSetting> Rules { get; set; } = [];
    public string UpdatedAt { get; set; } = "";
}

public class ApprovalRuleSetting
{
    /// <summary>Stable identifier (finance, legal, marketplace). Never edited by the user.</summary>
    public string Id { get; set; } = "";
    /// <summary>Reviewer step title shown on the approvals screen (e.g. "Finance Review").</summary>
    public string Title { get; set; } = "";
    /// <summary>Who the review is assigned to.</summary>
    public string Assignee { get; set; } = "";
    /// <summary>Whether this rule is active at all.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>
    /// How the rule decides it applies. Supported:
    /// <c>discountGreaterThan</c>, <c>durationMonthsGreaterThan</c>,
    /// <c>marketplacePresent</c>, <c>always</c>.
    /// </summary>
    public string ConditionType { get; set; } = "always";
    /// <summary>Numeric threshold for numeric conditions (e.g. 15 for "discount &gt; 15%").</summary>
    public decimal Threshold { get; set; }
    /// <summary>Engagement types this rule applies to. Empty = applies to every type.</summary>
    public List<string> EngagementTypes { get; set; } = [];
}
