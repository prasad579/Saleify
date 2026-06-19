using MarketplaceCopilot.Entities;

namespace MarketplaceCopilot.Services.Contracts;

public interface IDealHistoryService
{
    void Log(Deal deal, string category, string summary, string? details = null, string? changedBy = null);
    void LogCreated(Deal deal);
    void LogDealUpdated(Deal before, Deal after);
    void LogProducts(Deal deal, int productCount);
    void LogPricing(Deal deal);
    void LogMeetingNotes(Deal deal, int sessionsAdded, int actionCount, int reminderCount);
}
