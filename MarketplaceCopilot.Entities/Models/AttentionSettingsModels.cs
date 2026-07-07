namespace MarketplaceCopilot.Entities;

/// <summary>
/// Customizable behaviour for the home "needs attention" alert and the "upcoming this week" card.
/// Editable from Settings → Alerts &amp; Reminders so a user can turn each on/off, choose how many
/// days ahead "upcoming" looks, and pick which sources count (action items, reminders, engagement
/// close dates).
/// </summary>
public class AttentionSettings
{
    /// <summary>Show the top-of-home alert for overdue + due-today items.</summary>
    public bool AlertEnabled { get; set; } = true;
    /// <summary>Show the "Upcoming this week" card for items due in the next few days.</summary>
    public bool UpcomingEnabled { get; set; } = true;
    /// <summary>How many days ahead the "upcoming" card looks (1–30).</summary>
    public int UpcomingWindowDays { get; set; } = 7;

    /// <summary>Whether action items count towards the alert / upcoming card.</summary>
    public bool IncludeTasks { get; set; } = true;
    /// <summary>Whether reminders count.</summary>
    public bool IncludeReminders { get; set; } = true;
    /// <summary>Whether engagement target-close dates count.</summary>
    public bool IncludeEngagements { get; set; } = true;

    public string UpdatedAt { get; set; } = "";
}
