namespace MarketplaceCopilot.Entities;

/// <summary>
/// Customizable catalog of engagement types and the flow each one drives.
/// Editable from Settings → Engagement Types so an admin can enable/disable a type and
/// decide which sections (Products, Pricing, Meeting Notes, Approvals) apply to it —
/// e.g. turn approvals off for a Free Trial — without a code change. Drives the
/// engagement-creation picker, the deal stepper, and the lookups list.
/// </summary>
public class EngagementTypeSettings
{
    public List<EngagementTypeSetting> Types { get; set; } = [];
    public string UpdatedAt { get; set; } = "";
}

public class EngagementTypeSetting
{
    /// <summary>Display name and stable key for the engagement type (e.g. "Free Trial").</summary>
    public string Type { get; set; } = "";

    /// <summary>One-line description shown on the engagement-creation picker.</summary>
    public string Blurb { get; set; } = "";

    /// <summary>Whether the type is offered at all. Disabled types disappear from the picker and lookups.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Section applicability — "yes" (required step), "optional" (shown, skippable), or "no" (hidden).</summary>
    public string Products { get; set; } = "yes";
    public string Pricing { get; set; } = "yes";
    public string MeetingNotes { get; set; } = "yes";

    /// <summary>Whether the approvals step applies. "no" means approval is not required for this type.</summary>
    public string Approvals { get; set; } = "yes";

    /// <summary>Label of the final action button on the last applicable screen.</summary>
    public string SubmitLabel { get; set; } = "Submit to SaaSify";

    /// <summary>What the final action does: submit | complete | convert-later.</summary>
    public string SubmitAction { get; set; } = "submit";

    /// <summary>Whether a campaign / event tag must be chosen when creating the engagement.</summary>
    public bool TagRequired { get; set; }

    /// <summary>Whether a marketplace must be selected (internal/external leads carry no marketplace offer).</summary>
    public bool MarketplaceRequired { get; set; } = true;
}
