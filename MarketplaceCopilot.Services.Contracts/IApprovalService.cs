using MarketplaceCopilot.Entities;

namespace MarketplaceCopilot.Services.Contracts;

public interface IApprovalService
{
    ApprovalData EnsurePlan(Deal deal);
    void HandlePricingChange(Deal deal, string? newFingerprint = null);
    void HandleProductsChange(Deal deal);
    ApprovalActionResult ApplyAction(Deal deal, ApprovalActionRequest request);
    void TryCompleteApprovals(Deal deal, string? user = null);
    bool UnlockForEdits(Deal deal, string? user = null);
    void LogDocumentView(Deal deal, string docId, string? user = null);
    void LogDocumentDownload(Deal deal, string docId, string? user = null);
    string GetDocumentHtml(Deal deal, string docId);
    ApprovalData RegenerateDocuments(Deal deal, string? user = null);
    ApprovalSummary BuildSummary(Deal deal);
    bool CanProceed(Deal deal);
}
