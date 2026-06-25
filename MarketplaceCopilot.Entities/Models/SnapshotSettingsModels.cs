namespace MarketplaceCopilot.Entities;

/// <summary>
/// Settings that control the Engagement Snapshot / Email Summary feature.
/// Editable from Settings → Snapshot &amp; Email so the buttons can be turned on/off and the
/// snapshot/email sections and fields can be added, removed, renamed, or excluded from email
/// without a code change.
/// </summary>
public class SnapshotSettings
{
    /// <summary>Show the "Generate Snapshot" buttons across the app.</summary>
    public bool SnapshotButtonEnabled { get; set; } = true;
    /// <summary>Show the "Email Summary" buttons across the app.</summary>
    public bool EmailButtonEnabled { get; set; } = true;
    /// <summary>Optional line shown at the top of the email body.</summary>
    public string EmailIntro { get; set; } = "";
    /// <summary>Optional line shown at the bottom of the email body.</summary>
    public string EmailFooter { get; set; } = "";
    public List<SnapshotSectionSetting> Sections { get; set; } = [];
    public string UpdatedAt { get; set; } = "";
}

public class SnapshotSectionSetting
{
    /// <summary>Stable identifier — never edited by the user (eventInfo, engagementSummary, pipelineSummary, attention, privateOffers).</summary>
    public string Key { get; set; } = "";
    /// <summary>Customizable heading shown in the snapshot and email.</summary>
    public string Title { get; set; } = "";
    /// <summary>Show this section in the on-screen snapshot.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>Include this section in the email summary.</summary>
    public bool InEmail { get; set; } = true;
    public List<SnapshotFieldSetting> Fields { get; set; } = [];
}

public class SnapshotFieldSetting
{
    /// <summary>Stable identifier for the field/column.</summary>
    public string Key { get; set; } = "";
    /// <summary>Customizable label / column header.</summary>
    public string Label { get; set; } = "";
    public bool Enabled { get; set; } = true;
}
