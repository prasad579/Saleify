namespace MarketplaceCopilot.Entities;

/// <summary>
/// Configurable "what's next" guidance shown when an engagement type is selected.
/// Editable from the Settings → Playbooks screen so wording can be fixed without a code change.
/// </summary>
public class EngagementPlaybook
{
    public string EngagementType { get; set; } = "";
    public string Headline { get; set; } = "";
    public List<string> NextSteps { get; set; } = new();
    public List<string> TalkingPoints { get; set; } = new();
    public string Timeline { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
}
