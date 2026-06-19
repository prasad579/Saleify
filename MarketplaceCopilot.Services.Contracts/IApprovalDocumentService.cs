using MarketplaceCopilot.Entities;

namespace MarketplaceCopilot.Services.Contracts;

public interface IApprovalDocumentService
{
    void GenerateAll(Deal deal, string version, string timestamp, bool allApproved);
    ApprovalDocument? GetDocument(Deal deal, string docId);
    string GetHtml(Deal deal, string docId);
}
