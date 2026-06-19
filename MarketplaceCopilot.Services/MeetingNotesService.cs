using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;

namespace MarketplaceCopilot.Services;

public class MeetingNotesService : IMeetingNotesService
{
    public void Normalize(Deal deal)
    {
        if (deal.MeetingNotes is null) return;

        var notes = deal.MeetingNotes;
        notes.Sessions ??= [];
        notes.ActionItems ??= [];
        notes.Reminders ??= [];

        if (notes.Sessions.Count == 0 && !string.IsNullOrWhiteSpace(notes.RawNotes))
        {
            notes.Sessions.Add(new MeetingNoteSession
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                Title = "Initial meeting",
                RawNotes = notes.RawNotes,
                Extracted = notes.Extracted,
                CreatedAt = string.IsNullOrWhiteSpace(deal.CreatedAt)
                    ? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss") + "Z"
                    : deal.CreatedAt + "T12:00:00Z"
            });
        }

        foreach (var item in notes.ActionItems.Where(i => string.IsNullOrWhiteSpace(i.Id)))
            item.Id = Guid.NewGuid().ToString("N")[..8];

        foreach (var r in notes.Reminders.Where(r => string.IsNullOrWhiteSpace(r.Id)))
            r.Id = Guid.NewGuid().ToString("N")[..8];
    }

    public int ApplySave(Deal deal, SaveMeetingNotesRequest request)
    {
        deal.MeetingNotes ??= new MeetingNotesData();
        var notes = deal.MeetingNotes;
        notes.Sessions ??= [];
        var sessionsAdded = 0;

        if (request.NewSession is not null && !string.IsNullOrWhiteSpace(request.NewSession.RawNotes))
        {
            var session = request.NewSession;
            if (string.IsNullOrWhiteSpace(session.Id))
                session.Id = Guid.NewGuid().ToString("N")[..8];
            if (string.IsNullOrWhiteSpace(session.CreatedAt))
                session.CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss") + "Z";
            if (string.IsNullOrWhiteSpace(session.Title))
                session.Title = $"Meeting — {DateTime.UtcNow:MMM d, yyyy}";

            notes.Sessions.Insert(0, session);
            sessionsAdded = 1;
            notes.RawNotes = session.RawNotes;
            notes.Extracted = session.Extracted;
        }

        if (request.Sessions is not null)
            notes.Sessions = request.Sessions;

        if (request.ActionItems is not null)
            notes.ActionItems = request.ActionItems;

        if (request.Reminders is not null)
            notes.Reminders = request.Reminders;

        Normalize(deal);
        return sessionsAdded;
    }

    public List<ActionItem> MergeActionItems(IEnumerable<ActionItem> existing, IEnumerable<ActionItem> suggested)
    {
        var merged = existing.ToList();
        var seen = new HashSet<string>(merged.Select(i => i.Task.Trim().ToLowerInvariant()));

        foreach (var item in suggested)
        {
            var key = item.Task.Trim().ToLowerInvariant();
            if (!seen.Add(key)) continue;
            if (string.IsNullOrWhiteSpace(item.Id))
                item.Id = Guid.NewGuid().ToString("N")[..8];
            item.Source ??= "ai";
            merged.Add(item);
        }

        return merged;
    }
}
