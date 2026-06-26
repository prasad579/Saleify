using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;

namespace MarketplaceCopilot.Services;

public class ApprovalService(IDealHistoryService history, IApprovalDocumentService documents, DataStore store, IAuditService audit) : IApprovalService
{
    private const string DefaultUser = "Srinivas K";

    private const string ConsumptionNoteText =
        "This is a free trial with no upfront charge. On crossing the included consumption limit, " +
        "the customer will be automatically charged at standard list rates as per the EULA.";

    /// <summary>True when the deal carries no contract value (free trial / no-money offer).</summary>
    public static bool IsNoMoneyDeal(Deal deal)
    {
        var offerType = deal.Pricing?.OfferType;
        if (PricingService.IsNoMoneyOffer(offerType)) return true;
        return deal.Pricing is not null && deal.Pricing.NetContractValue <= 0;
    }

    public ApprovalData EnsurePlan(Deal deal)
    {
        deal.Approvals ??= new ApprovalData();
        var fp = BuildPricingFingerprint(deal);
        if (deal.Approvals.Steps.Count == 0)
        {
            deal.Approvals.PricingFingerprint = fp;
            deal.Approvals.Version = "V1.0";
            deal.Approvals.LastUpdatedAt = Now();
            deal.Approvals.LastUpdatedBy = DefaultUser;
            RebuildPlan(deal);
            GenerateDocuments(deal, isRegeneration: false);
            LogAudit(deal, "Approval", "Approval plan initialized", "Required approval steps generated from deal rules.", DefaultUser);
        }
        else if (deal.Approvals.PricingFingerprint != fp)
        {
            HandlePricingChange(deal, fp);
        }

        RefreshRuleMatches(deal);
        if (!deal.Approvals.DocumentsLocked)
        {
            var ts = string.IsNullOrWhiteSpace(deal.Approvals.LastUpdatedAt) ? Now() : deal.Approvals.LastUpdatedAt;
            documents.GenerateAll(deal, deal.Approvals.Version, ts, AreRequiredStepsApproved(deal));
        }
        return deal.Approvals;
    }

    public void HandlePricingChange(Deal deal, string? newFingerprint = null)
    {
        var fp = newFingerprint ?? BuildPricingFingerprint(deal);
        deal.Approvals ??= new ApprovalData();
        if (deal.Approvals.Steps.Count == 0)
        {
            deal.Approvals.PricingFingerprint = fp;
            return;
        }

        if (string.IsNullOrEmpty(deal.Approvals.PricingFingerprint))
        {
            deal.Approvals.PricingFingerprint = fp;
            return;
        }

        if (deal.Approvals.PricingFingerprint == fp) return;

        var oldFp = deal.Approvals.PricingFingerprint;
        deal.Approvals.PricingFingerprint = fp;
        deal.Approvals.ChangesPendingReapproval = true;
        deal.Approvals.ChangeSummary = DescribePricingChange(deal, oldFp, fp);
        deal.Approvals.Version = BumpVersion(deal.Approvals.Version);

        // Any approved reviewer step (everything except the auto EULA step) needs re-approval.
        foreach (var step in deal.Approvals.Steps.Where(s =>
            s.Status is "Approved" or "Pending" && s.Id != "eula"))
        {
            if (step.Status == "Approved")
            {
                step.Status = "Needs Re-approval";
                step.CompletedAt = null;
            }
        }

        MarkDocumentsStale(deal);
        deal.Approvals.DocumentsLocked = false;
        GenerateDocuments(deal, isRegeneration: true);
        LogAudit(deal, "System", "Changes detected",
            deal.Approvals.ChangeSummary ?? "Pricing or product configuration changed.",
            "System");
        history.Log(deal, "Approvals",
            "Pricing/product change — documents regenerated, approvals need re-review.",
            deal.Approvals.ChangeSummary);
    }

    public void HandleProductsChange(Deal deal)
    {
        var fp = BuildPricingFingerprint(deal);
        if (deal.Approvals?.Steps.Count > 0)
            HandlePricingChange(deal, fp);
    }

    public ApprovalActionResult ApplyAction(Deal deal, ApprovalActionRequest request)
    {
        EnsurePlan(deal);
        var step = deal.Approvals!.Steps.FirstOrDefault(s => s.Id == request.StepId);
        if (step is null) return new ApprovalActionResult { Success = false, Message = "Approval step not found." };

        var user = string.IsNullOrWhiteSpace(request.User) ? DefaultUser : request.User.Trim();
        var comment = request.Comment?.Trim() ?? "";

        switch (request.Action?.ToLowerInvariant())
        {
            case "approve":
                step.Status = "Approved";
                step.CompletedAt = Now();
                if (!string.IsNullOrEmpty(comment))
                    AddComment(step, user, comment, "approval");
                LogAudit(deal, "Approval", $"{step.Title} approved", comment, user, step.Title);
                history.Log(deal, "Approvals", $"{step.Title} approved.", comment);
                audit.LogDeal(deal, "Approvals", $"{step.Title} approved", comment, user);
                break;
            case "reject":
                if (string.IsNullOrEmpty(comment))
                    return new ApprovalActionResult { Success = false, Message = "Comment required when rejecting." };
                step.Status = "Rejected";
                step.CompletedAt = Now();
                AddComment(step, user, comment, "rejection");
                LogAudit(deal, "Approval", $"{step.Title} rejected", comment, user, step.Title);
                history.Log(deal, "Approvals", $"{step.Title} rejected.", comment);
                audit.LogDeal(deal, "Approvals", $"{step.Title} rejected", comment, user);
                break;
            case "request-changes":
                if (string.IsNullOrEmpty(comment))
                    return new ApprovalActionResult { Success = false, Message = "Comment required when requesting changes." };
                step.Status = "Changes Requested";
                step.CompletedAt = null;
                AddComment(step, user, comment, "changes");
                LogAudit(deal, "Approval", "Changes requested", comment, user, step.Title);
                history.Log(deal, "Approvals", $"{step.Title} — changes requested.", comment);
                audit.LogDeal(deal, "Approvals", $"{step.Title} — changes requested", comment, user);
                break;
            case "mark-ready":
                if (step.Status != "Changes Requested")
                    return new ApprovalActionResult { Success = false, Message = "Step is not awaiting changes." };
                step.Status = "Pending";
                AddComment(step, user, comment.Length > 0 ? comment : "Marked ready for re-review.", "response");
                LogAudit(deal, "Approval", "Marked ready for re-review", comment, user, step.Title);
                history.Log(deal, "Approvals", $"{step.Title} marked ready for re-review.", comment);
                audit.LogDeal(deal, "Approvals", $"{step.Title} marked ready for re-review", comment, user);
                break;
            default:
                return new ApprovalActionResult { Success = false, Message = "Unknown action." };
        }

        deal.Approvals.LastUpdatedAt = Now();
        deal.Approvals.LastUpdatedBy = user;
        TryCompleteApprovals(deal, user);
        return new ApprovalActionResult { Success = true, Message = "Approval updated.", Approvals = deal.Approvals };
    }

    public void TryCompleteApprovals(Deal deal, string? user = null)
    {
        if (!CanProceed(deal)) return;
        var ts = Now();
        documents.GenerateAll(deal, deal.Approvals!.Version, ts, allApproved: true);
        deal.Approvals.DocumentsLocked = true;
        deal.Approvals.EulaStatus = "Final";
        var eulaStep = deal.Approvals.Steps.FirstOrDefault(s => s.Id == "eula");
        if (eulaStep != null)
        {
            eulaStep.Status = "Approved";
            eulaStep.CompletedAt = ts;
        }
        LogAudit(deal, "Document", "Documents locked", "All approvals complete — package and EULA finalized.", user ?? DefaultUser, documentName: "Approval Package");
        history.Log(deal, "Approvals", "All approvals complete — documents locked.", deal.Approvals.Version);
    }

    public bool UnlockForEdits(Deal deal, string? user = null)
    {
        if (deal.Approvals is null) return false;
        if (!deal.Approvals.AllowPostApprovalEdits)
            return false;
        deal.Approvals.DocumentsLocked = false;
        foreach (var doc in deal.Approvals.Documents)
            doc.Locked = false;
        deal.Approvals.EulaStatus = "Partial";
        LogAudit(deal, "System", "Unlocked for edits", "Documents unlocked to allow post-approval changes. Re-approval required after edits.", user ?? DefaultUser);
        history.Log(deal, "Approvals", "Documents unlocked for post-approval edits.", "");
        return true;
    }

    public void LogDocumentView(Deal deal, string docId, string? user = null)
    {
        var doc = documents.GetDocument(deal, docId);
        if (doc is null) return;
        LogAudit(deal, "Document", "Document viewed", $"Opened snapshot: {doc.Name} ({doc.Version})", user ?? DefaultUser, documentName: doc.Name);
    }

    public void LogDocumentDownload(Deal deal, string docId, string? user = null)
    {
        var doc = documents.GetDocument(deal, docId);
        if (doc is null) return;
        LogAudit(deal, "Document", "Document downloaded", $"Downloaded: {doc.FileName}", user ?? DefaultUser, documentName: doc.Name);
    }

    public string GetDocumentHtml(Deal deal, string docId) => documents.GetHtml(deal, docId);

    public ApprovalData RegenerateDocuments(Deal deal, string? user = null)
    {
        EnsurePlan(deal);
        if (deal.Approvals!.DocumentsLocked)
            return deal.Approvals;
        deal.Approvals.Version = BumpVersion(deal.Approvals.Version);
        GenerateDocuments(deal, isRegeneration: true);
        deal.Approvals.LastUpdatedAt = Now();
        deal.Approvals.LastUpdatedBy = user ?? DefaultUser;
        LogAudit(deal, "Document", "Documents regenerated", $"All documents regenerated at {deal.Approvals.Version}.", user ?? DefaultUser, documentName: "All documents");
        history.Log(deal, "Approvals", "Approval documents regenerated.", deal.Approvals.Version);
        return deal.Approvals;
    }

    public ApprovalSummary BuildSummary(Deal deal)
    {
        EnsurePlan(deal);
        var steps = deal.Approvals!.Steps;
        return new ApprovalSummary
        {
            Version = deal.Approvals.Version,
            LastUpdatedAt = deal.Approvals.LastUpdatedAt,
            LastUpdatedBy = deal.Approvals.LastUpdatedBy,
            ChangesPendingReapproval = deal.Approvals.ChangesPendingReapproval,
            ChangeSummary = deal.Approvals.ChangeSummary,
            DocumentsLocked = deal.Approvals.DocumentsLocked,
            AllowPostApprovalEdits = deal.Approvals.AllowPostApprovalEdits,
            EulaStatus = deal.Approvals.EulaStatus,
            IsNoMoneyDeal = IsNoMoneyDeal(deal),
            ConsumptionNote = IsNoMoneyDeal(deal) ? ConsumptionNoteText : null,
            PackageSummaryId = deal.Approvals.PackageSummaryId,
            NextStep = steps.FirstOrDefault(s => s.Status is "Pending" or "Changes Requested" or "Needs Re-approval")?.Title ?? "Complete",
            RuleMatches = deal.Approvals.RuleMatches,
            Steps = steps,
            Documents = deal.Approvals.Documents,
            AuditLog = deal.Approvals.AuditLog,
            Progress = BuildProgress(steps),
            EulaDocument = deal.Approvals.Documents.FirstOrDefault(d => d.DocumentType == "eula"),
            PackageDocument = deal.Approvals.Documents.FirstOrDefault(d => d.Id == deal.Approvals.PackageSummaryId)
        };
    }

    public bool CanProceed(Deal deal) => AreRequiredStepsApproved(deal);

    private static bool AreRequiredStepsApproved(Deal deal)
    {
        if (deal.Approvals?.Steps is null || deal.Approvals.Steps.Count == 0) return false;
        var required = deal.Approvals.Steps.Where(s => s.Id != "eula").ToList();
        return required.Count > 0 && required.All(s => s.Status == "Approved");
    }

    private void RebuildPlan(Deal deal)
    {
        var rules = EvaluateRules(deal);
        deal.Approvals!.RuleMatches = rules;
        var steps = new List<ApprovalStep>();
        var order = 1;

        // One reviewer step per matched rule, in the order the rules are configured in Settings.
        foreach (var rule in rules.Where(r => r.Matched))
        {
            steps.Add(new ApprovalStep
            {
                Id = rule.Id,
                Order = order++,
                Title = string.IsNullOrWhiteSpace(rule.Title) ? rule.Id : rule.Title,
                Assignee = string.IsNullOrWhiteSpace(rule.Assignee) ? "Unassigned" : rule.Assignee,
                Status = "Pending",
                RuleReason = rule.Reason
            });
        }

        steps.Add(new ApprovalStep
        {
            Id = "eula",
            Order = order,
            Title = "Generate EULA & Final Package",
            Assignee = "System",
            Status = "Pending",
            RuleReason = IsNoMoneyDeal(deal)
                ? ConsumptionNoteText
                : "Auto-triggered after all approvals complete."
        });

        deal.Approvals.Steps = steps;
    }

    /// <summary>
    /// Evaluate the configurable approval rules (Settings → Approval Rules) against the deal.
    /// Only rules that are enabled and apply to the deal's engagement type are considered.
    /// </summary>
    private List<ApprovalRuleMatch> EvaluateRules(Deal deal)
    {
        var p = deal.Pricing;
        var discount = p?.DiscountPercent ?? 0;
        var durationMonths = p?.DurationMonths ?? 0;
        if (durationMonths <= 0 && p?.DurationType == "years")
            durationMonths = (p?.DurationValue ?? 0) * 12;
        if (durationMonths <= 0 && p?.DurationType == "months")
            durationMonths = p?.DurationValue ?? 0;

        var noMoney = IsNoMoneyDeal(deal);
        var type = deal.EngagementType ?? "";
        var matches = new List<ApprovalRuleMatch>();

        foreach (var rule in store.ApprovalRulesSettings.Rules)
        {
            if (!rule.Enabled) continue;
            if (rule.EngagementTypes.Count > 0 &&
                !rule.EngagementTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
                continue;

            bool matched;
            string reason;
            switch ((rule.ConditionType ?? "always").ToLowerInvariant())
            {
                case "discountgreaterthan":
                    matched = !noMoney && discount > rule.Threshold;
                    reason = noMoney
                        ? "Skipped — no contract value for a free trial."
                        : matched
                            ? $"Discount exceeds {rule.Threshold:0.##}% (current: {discount:0.##}%)."
                            : $"Discount within policy ({discount:0.##}%, limit {rule.Threshold:0.##}%).";
                    break;

                case "durationmonthsgreaterthan":
                    matched = !noMoney && durationMonths > rule.Threshold;
                    reason = noMoney
                        ? "Skipped — standard EULA applies for the free trial."
                        : matched
                            ? $"Contract duration > {rule.Threshold:0.##} months ({durationMonths} months)."
                            : $"Contract duration within policy ({durationMonths} months, limit {rule.Threshold:0.##}).";
                    break;

                case "marketplacepresent":
                    matched = !string.IsNullOrWhiteSpace(deal.Marketplace);
                    reason = noMoney
                        ? $"Minimal approval — free trial activation on {deal.Marketplace}."
                        : matched
                            ? $"Marketplace: {deal.Marketplace}."
                            : "No marketplace selected.";
                    break;

                default: // "always"
                    matched = true;
                    reason = $"Required for all {(string.IsNullOrWhiteSpace(type) ? "" : type + " ")}engagements.";
                    break;
            }

            matches.Add(new ApprovalRuleMatch
            {
                Id = rule.Id,
                Title = rule.Title,
                Assignee = rule.Assignee,
                Reason = reason,
                Matched = matched
            });
        }

        return matches;
    }

    private void RefreshRuleMatches(Deal deal)
    {
        var rules = EvaluateRules(deal);
        deal.Approvals!.RuleMatches = rules;
        foreach (var step in deal.Approvals.Steps)
        {
            var rule = rules.FirstOrDefault(r => r.Id == step.Id);
            if (rule != null) step.RuleReason = rule.Reason;
        }
    }

    private void GenerateDocuments(Deal deal, bool isRegeneration)
    {
        var version = deal.Approvals!.Version;
        var ts = Now();
        documents.GenerateAll(deal, version, ts, AreRequiredStepsApproved(deal));
        if (isRegeneration)
            deal.Approvals.ChangesPendingReapproval = true;
    }

    private void MarkDocumentsStale(Deal deal)
    {
        foreach (var doc in deal.Approvals!.Documents)
            doc.Stale = true;
    }

    private static ApprovalProgress BuildProgress(List<ApprovalStep> steps)
    {
        var required = steps.Where(s => s.Id != "eula").ToList();
        var approved = required.Count(s => s.Status == "Approved");
        var total = required.Count;
        return new ApprovalProgress
        {
            Total = total,
            Approved = approved,
            Pending = required.Count(s => s.Status == "Pending"),
            ChangesRequested = required.Count(s => s.Status is "Changes Requested" or "Needs Re-approval"),
            Rejected = required.Count(s => s.Status == "Rejected"),
            Percent = total == 0 ? 0 : (int)Math.Round(approved * 100.0 / total)
        };
    }

    public static string BuildPricingFingerprint(Deal deal)
    {
        var p = deal.Pricing;
        var products = string.Join(",", deal.ProductIds.OrderBy(x => x));
        if (p is null) return $"nopricing|{products}";
        return string.Join("|",
            p.DiscountPercent.ToString("0.##"),
            p.AbsoluteContractPrice.ToString("0.##"),
            p.PricingMethod ?? "",
            p.DurationType ?? "",
            p.DurationValue,
            p.DurationMonths,
            p.FlexiblePaymentsEnabled,
            p.InstallmentCount,
            p.ProRateEnabled,
            products);
    }

    private static string DescribePricingChange(Deal deal, string oldFp, string newFp)
    {
        var parts = new List<string>();
        var p = deal.Pricing;
        if (p != null)
        {
            parts.Add($"Discount: {p.DiscountPercent}%");
            parts.Add($"Net value: ${p.NetContractValue:N0}");
            if (p.FlexiblePaymentsEnabled)
                parts.Add($"Installments: {p.InstallmentCount}");
            if (deal.ProductIds.Count > 0)
                parts.Add($"Products: {deal.ProductIds.Count} selected");
        }
        return "Pricing or product plan changed — " + string.Join("; ", parts);
    }

    private static string BumpVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version)) return "V1.1";
        var numPart = version.TrimStart('V', 'v');
        if (decimal.TryParse(numPart, out var v))
            return $"V{(v + 0.1m):0.0}";
        return "V1.1";
    }

    private static void AddComment(ApprovalStep step, string author, string text, string type)
    {
        step.Comments.Add(new ApprovalComment
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Author = author,
            Text = text,
            Timestamp = Now(),
            Type = type
        });
        step.Comment = text;
    }

    private void LogAudit(Deal deal, string category, string action, string details, string user,
        string stepTitle = "", string documentName = "")
    {
        deal.Approvals!.AuditLog.Insert(0, new ApprovalAuditEntry
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            Timestamp = Now(),
            Category = category,
            User = user,
            Action = action,
            Details = details,
            StepTitle = stepTitle,
            DocumentName = documentName
        });
    }

    private static string Now() => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
}
