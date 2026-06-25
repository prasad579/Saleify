using System.Globalization;

namespace MarketplaceCopilot.Entities;

/// <summary>
/// A campaign or event tag (e.g. "AWS Summit 2026") that deals can be associated with.
/// Status is derived from the current date relative to the event window.
/// </summary>
public class CampaignEvent
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Marketplace { get; set; } = "";
    public string StartDate { get; set; } = "";   // yyyy-MM-dd
    public string EndDate { get; set; } = "";      // yyyy-MM-dd
    public string Description { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    /// <summary>On hold — hidden from the tag dropdown when creating an engagement (still managed in Settings).</summary>
    public bool Paused { get; set; }

    /// <summary>Upcoming | Active | Completed — computed, never persisted as source of truth.</summary>
    public string Status => ComputeStatus(StartDate, EndDate, DateTime.UtcNow.Date);

    public static string ComputeStatus(string startDate, string endDate, DateTime today)
    {
        var start = ParseDate(startDate);
        var end = ParseDate(endDate);

        if (start.HasValue && today < start.Value) return "Upcoming";
        if (end.HasValue && today > end.Value) return "Completed";
        return "Active";
    }

    private static DateTime? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? parsed.Date
            : null;
    }
}

/// <summary>Funnel showing how an event converts into deals and downstream engagements.</summary>
public class ConversionFunnel
{
    public string EventId { get; set; } = "";
    public string EventName { get; set; } = "";
    public int TotalDeals { get; set; }
    public int ClosedWon { get; set; }
    public List<FunnelStage> Stages { get; set; } = [];
}

public class FunnelStage
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
}
