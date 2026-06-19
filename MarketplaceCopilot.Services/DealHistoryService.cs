using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;

namespace MarketplaceCopilot.Services;

public class DealHistoryService : IDealHistoryService
{
    private const string DefaultUser = "Srinivas K";

    public void Log(Deal deal, string category, string summary, string? details = null, string? changedBy = null)
    {
        deal.ChangeHistory ??= [];
        deal.ChangeHistory.Insert(0, new DealChangeEntry
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
            Category = category,
            Summary = summary,
            Details = details ?? "",
            ChangedBy = changedBy ?? DefaultUser
        });
    }

    public void LogCreated(Deal deal) =>
        Log(deal, "Deal", $"Deal {deal.Id} created for {deal.Customer}.", $"Marketplace: {deal.Marketplace}");

    public void LogDealUpdated(Deal before, Deal after)
    {
        var changes = new List<string>();
        if (before.Name != after.Name) changes.Add($"Name: {before.Name} → {after.Name}");
        if (before.Customer != after.Customer) changes.Add($"Customer: {before.Customer} → {after.Customer}");
        if (before.Marketplace != after.Marketplace) changes.Add($"Marketplace: {before.Marketplace} → {after.Marketplace}");
        if (before.ExpectedValue != after.ExpectedValue) changes.Add($"Expected value: ${before.ExpectedValue:N0} → ${after.ExpectedValue:N0}");
        if (before.ExpectedCloseDate != after.ExpectedCloseDate) changes.Add($"Close date: {before.ExpectedCloseDate} → {after.ExpectedCloseDate}");
        if (before.DealType != after.DealType) changes.Add($"Deal type: {before.DealType} → {after.DealType}");
        if (changes.Count == 0) changes.Add("Deal details updated.");

        Log(after, "Deal", $"Deal {after.Id} information updated.", string.Join("; ", changes));
    }

    public void LogProducts(Deal deal, int productCount) =>
        Log(deal, "Products", $"{productCount} product(s) selected for {deal.Id}.", string.Join(", ", deal.ProductIds));

    public void LogPricing(Deal deal) =>
        Log(deal, "Pricing", $"Pricing configured — net ${deal.Pricing?.NetContractValue:N0}, {deal.Pricing?.DiscountPercent}% discount.",
            $"Duration: {deal.Pricing?.DurationValue} {deal.Pricing?.DurationType}");

    public void LogMeetingNotes(Deal deal, int sessionsAdded, int actionCount, int reminderCount)
    {
        var sessionTotal = deal.MeetingNotes?.Sessions?.Count ?? 0;
        Log(deal, "Meeting Notes",
            sessionsAdded > 0
                ? $"Added meeting summary #{sessionTotal} with {actionCount} action item(s) and {reminderCount} reminder(s)."
                : $"Updated meeting notes — {actionCount} action item(s), {reminderCount} reminder(s).",
            sessionsAdded > 0 ? deal.MeetingNotes?.Sessions?.FirstOrDefault()?.Title : null);
    }
}
