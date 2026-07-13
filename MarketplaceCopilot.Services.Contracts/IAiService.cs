using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;

namespace MarketplaceCopilot.Services.Contracts;

public interface IAiService
{
    AiExtractedSummary ExtractInsights(string notes);
    List<ActionItem> SuggestActionItems(string notes);
    string BuildInsight(string notes, AiExtractedSummary summary);
    string Chat(string message, Deal? deal, DataStore store, string tenantId);
}
