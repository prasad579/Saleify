using MarketplaceCopilot.Entities;

namespace MarketplaceCopilot.Services.Contracts;

public interface IMeetingNotesService
{
    void Normalize(Deal deal);
    int ApplySave(Deal deal, SaveMeetingNotesRequest request);
    List<ActionItem> MergeActionItems(IEnumerable<ActionItem> existing, IEnumerable<ActionItem> suggested);
}
