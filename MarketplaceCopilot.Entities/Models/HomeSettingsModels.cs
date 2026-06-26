namespace MarketplaceCopilot.Entities;

/// <summary>
/// Customizable home / dashboard layout. Editable from Settings → Home Dashboard so each card
/// (stats, insights, tags, open engagements, recent activity, tasks, reminders) can be shown or
/// hidden for quick access — without a code change.
/// </summary>
public class HomeSettings
{
    public List<HomeCardSetting> Cards { get; set; } = [];
    public string UpdatedAt { get; set; } = "";
}

public class HomeCardSetting
{
    /// <summary>Stable key matching the section in the home component (e.g. "insights", "tasks").</summary>
    public string Key { get; set; } = "";
    /// <summary>Display name shown in the settings list.</summary>
    public string Label { get; set; } = "";
    /// <summary>Short description of what the card shows.</summary>
    public string Description { get; set; } = "";
    /// <summary>Whether the card is shown on the home page.</summary>
    public bool Enabled { get; set; } = true;
}
