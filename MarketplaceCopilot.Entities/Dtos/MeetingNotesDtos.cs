namespace MarketplaceCopilot.Entities;

public class SaveMeetingNotesRequest
{
    public MeetingNoteSession? NewSession { get; set; }
    public List<MeetingNoteSession>? Sessions { get; set; }
    public List<ActionItem>? ActionItems { get; set; }
    public List<DealReminder>? Reminders { get; set; }
}
