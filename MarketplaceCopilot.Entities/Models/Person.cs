namespace MarketplaceCopilot.Entities;

/// <summary>
/// A person who can own engagements. Managed from Settings → People (enable/disable,
/// role, and which engagement types they can own). Designed so an external tenant /
/// directory sync can populate these later — <see cref="Source"/> marks the origin.
/// </summary>
public class Person
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    /// <summary>Disabled people are hidden from the engagement owner dropdown.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>Engagement types this person can own; empty = eligible for all types.</summary>
    public List<string> EngagementTypes { get; set; } = [];
    /// <summary>"manual" or "tenant" — origin of the record (tenant sync is a future integration).</summary>
    public string Source { get; set; } = "manual";
}
