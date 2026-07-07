using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services;
using MarketplaceCopilot.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DealsController(
    DataStore store,
    IDealService dealService,
    IPricingService pricingService,
    IDealHistoryService history,
    IMeetingNotesService meetingNotes,
    IApprovalService approvals,
    IAuditService audit,
    IOfferRequestService offerRequests) : ControllerBase
{
    /// <summary>
    /// List engagements. By default only active (non-archived) engagements are returned;
    /// pass ?view=archived for archived only, or ?view=all for both.
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<Deal>> GetAll([FromQuery] string? view = null)
    {
        foreach (var d in store.Deals)
            meetingNotes.Normalize(d);

        IEnumerable<Deal> deals = (view ?? "").ToLowerInvariant() switch
        {
            "archived" => store.Deals.Where(d => d.Archived),
            "all" => store.Deals,
            _ => store.Deals.Where(d => !d.Archived)
        };
        return deals.ToList();
    }

    [HttpGet("stats")]
    public ActionResult<DealStats> GetStats()
    {
        var deals = store.Deals.Where(d => !d.Archived).ToList();
        return new DealStats
        {
            Total = deals.Count,
            Draft = deals.Count(d => d.MarketplaceStatus == "Draft"),
            InProgress = deals.Count(d => d.Stage is "Pricing" or "Discovery" or "Approval" or "Product Selection" or "Meeting Notes"),
            Submitted = deals.Count(d => d.MarketplaceStatus == "In Review"),
            Accepted = deals.Count(d => d.MarketplaceStatus == "Published"),
            Abandoned = deals.Count(d => d.MarketplaceStatus == "Abandoned")
        };
    }

    [HttpGet("{id}")]
    public ActionResult<DealDetailDto> GetById(string id)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();
        meetingNotes.Normalize(deal);
        return dealService.ToDetail(deal);
    }

    [HttpGet("{id}/history")]
    public ActionResult<object> GetHistory(string id)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();
        return new { dealId = id, changeHistory = deal.ChangeHistory ?? [] };
    }

    /// <summary>Soft-hide an engagement (and its tasks/reminders) from active views. Restorable.</summary>
    [HttpPost("{id}/archive")]
    public ActionResult<object> Archive(string id)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();

        deal.Archived = true;
        deal.ArchivedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'");
        deal.LastUpdated = "Just now";
        history.Log(deal, "Engagement", $"Engagement {deal.Id} archived.",
            "Hidden from dashboards and lists, along with its tasks and reminders. Can be restored.");
        audit.LogDeal(deal, "Engagement", "Engagement archived", "Hidden from active views; restorable.");
        store.SaveDeals();
        return new { success = true, message = $"{DealLabel(deal)} archived. You can restore it from the Archived view." };
    }

    /// <summary>Restore an archived engagement back into active views.</summary>
    [HttpPost("{id}/unarchive")]
    public ActionResult<object> Unarchive(string id)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();

        deal.Archived = false;
        deal.ArchivedAt = "";
        deal.LastUpdated = "Just now";
        history.Log(deal, "Engagement", $"Engagement {deal.Id} restored.", "Returned to active views with its tasks and reminders.");
        audit.LogDeal(deal, "Engagement", "Engagement restored", "Returned to active views.");
        store.SaveDeals();
        return new { success = true, message = $"{DealLabel(deal)} restored." };
    }

    /// <summary>Permanently delete an engagement and everything attached to it. Cannot be undone.</summary>
    [HttpDelete("{id}")]
    public ActionResult<object> Delete(string id)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();

        store.Deals.Remove(deal);
        audit.LogDeal(deal, "Engagement", "Engagement permanently deleted", "Engagement and all attached data removed. Cannot be undone.");
        store.SaveDeals();
        return new { success = true, message = $"{DealLabel(deal)} permanently deleted." };
    }

    /// <summary>
    /// Unlock a submitted engagement so its details can be revised. Revising marks the offer request
    /// out of date until the engagement is re-submitted.
    /// </summary>
    [HttpPost("{id}/unlock-edits")]
    public ActionResult<object> UnlockEdits(string id)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();
        if (!deal.Locked) return new { success = true, message = "Engagement is already editable." };

        deal.Locked = false;
        deal.LastUpdated = "Just now";
        history.Log(deal, "Engagement", "Engagement unlocked for revision.",
            "Details were locked after submission. Re-submit after editing to push changes to the destination.");
        audit.LogDeal(deal, "Engagement", "Engagement unlocked for revision",
            "Was locked after submission as an offer request. Edits now allowed; re-submit to push changes.");
        store.SaveDeals();
        return new { success = true, message = $"{DealLabel(deal)} unlocked. Edit and then re-submit to push the changes." };
    }

    private static string DealLabel(Deal d) => string.IsNullOrWhiteSpace(d.Name) ? d.Id : d.Name;

    [HttpPost]
    public ActionResult<CreateDealResponse> Create([FromBody] CreateDealRequest request)
    {
        // Marketplace is derived from the tagged campaign / event when one is set (auto-fill + lock).
        request.Marketplace = DeriveMarketplace(request.CampaignEventId?.Trim() ?? "", request.Marketplace?.Trim() ?? "");

        var validationError = ValidateDeal(request);
        if (validationError is not null)
            return BadRequest(new CreateDealResponse { Success = false, Message = validationError });

        var customer = request.Customer?.Trim() ?? "";
        var name = string.IsNullOrWhiteSpace(request.Name)
            ? string.Join(" — ", new[] { request.EngagementType.Trim(), customer }.Where(s => !string.IsNullOrWhiteSpace(s)))
            : request.Name.Trim();

        var deal = new Deal
        {
            Id = store.NextDealId(),
            Name = string.IsNullOrWhiteSpace(name) ? request.EngagementType.Trim() : name,
            Customer = customer,
            ContactName = request.ContactName?.Trim() ?? "",
            ContactEmail = request.ContactEmail.Trim(),
            Phone = request.Phone?.Trim() ?? "",
            Priority = request.Priority?.Trim() ?? "",
            Location = request.Location?.Trim() ?? "",
            Industry = request.Industry?.Trim() ?? "",
            Marketplace = request.Marketplace.Trim(),
            DealType = string.IsNullOrWhiteSpace(request.DealType) ? "New Deal" : request.DealType.Trim(),
            EngagementType = request.EngagementType.Trim(),
            QuickCapture = request.QuickCapture,
            CampaignEventId = request.CampaignEventId.Trim(),
            CampaignEventName = request.CampaignEventName?.Trim() ?? "",
            BillingAccountId = request.BillingAccountId?.Trim() ?? "",
            BillingAccountIds = NormalizeAccountIds(request.BillingAccountIds),
            ExpectedValue = request.ExpectedValue,
            ExpectedCloseDate = request.ExpectedCloseDate?.Trim() ?? "",
            Description = request.Description?.Trim() ?? "",
            Stage = request.QuickCapture ? "Quick Capture" : "Draft",
            StepNumber = 1,
            TotalSteps = 5,
            MarketplaceStatus = "Draft",
            LastUpdated = "Just now",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Owner = request.Owner.Trim()
        };

        history.LogCreated(deal);
        audit.LogDeal(deal, "Engagement", "Engagement created",
            $"{deal.EngagementType} for {(string.IsNullOrWhiteSpace(deal.Customer) ? "—" : deal.Customer)}" +
            (string.IsNullOrWhiteSpace(deal.Marketplace) ? "" : $" on {deal.Marketplace}") +
            $", owner {deal.Owner}.");
        store.Deals.Insert(0, deal);
        store.SaveDeals();

        return Ok(new CreateDealResponse
        {
            Success = true,
            Message = $"Deal {deal.Id} created successfully.",
            Deal = deal
        });
    }

    private const string LockedMessage =
        "This engagement has been submitted as an offer request and is locked. Unlock it to revise — your changes will then need to be re-submitted.";

    [HttpPut("{id}")]
    public ActionResult<Deal> Update(string id, [FromBody] Deal updated)
    {
        var index = store.Deals.FindIndex(d => d.Id == id);
        if (index < 0) return NotFound();

        var existing = store.Deals[index];
        if (existing.Locked) return BadRequest(new { success = false, message = LockedMessage });
        var before = CloneDeal(existing);
        updated.Id = id;
        // Keep marketplace consistent with the tagged campaign / event (auto-fill + lock).
        updated.Marketplace = DeriveMarketplace(updated.CampaignEventId?.Trim() ?? "", updated.Marketplace?.Trim() ?? "");
        updated.LastUpdated = "Just now";
        updated.ChangeHistory = existing.ChangeHistory;
        updated.MeetingNotes = existing.MeetingNotes;
        updated.Approvals = existing.Approvals;
        updated.CreatedAt = existing.CreatedAt;
        // Preserve workflow progress + lock state across an edit (the form doesn't post these).
        updated.Pricing = existing.Pricing;
        updated.ProductIds = existing.ProductIds;
        updated.StepNumber = existing.StepNumber;
        updated.MarketplaceStatus = existing.MarketplaceStatus;
        updated.Stage = existing.Stage;
        updated.Locked = existing.Locked;
        updated.ChangedSinceSubmission = existing.ChangedSinceSubmission;
        // Completing a quick-capture engagement promotes it out of the Quick Capture stage.
        if (!updated.QuickCapture && existing.Stage == "Quick Capture")
            updated.Stage = "Draft";
        history.LogDealUpdated(before, updated);
        var changeDetail = updated.ChangeHistory.FirstOrDefault()?.Details ?? "Deal details updated.";
        // Reuse the field-level diff the history service just recorded as the audit detail.
        audit.LogDeal(updated, "Engagement", "Engagement details updated", changeDetail);
        // If already submitted (post-unlock edit), flag the offer request as out of date.
        offerRequests.MarkEngagementChanged(updated, $"Engagement details edited — {changeDetail}");
        store.Deals[index] = updated;
        store.SaveDeals();
        return updated;
    }

    [HttpPost("{id}/products")]
    public ActionResult<DealDetailDto> SetProducts(string id, [FromBody] List<string> productIds)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();
        if (deal.Locked) return BadRequest(new { success = false, message = LockedMessage });

        deal.ProductIds = productIds;
        var listPrice = dealService.GetListPriceFromProducts(deal);
        var feePercent = PricingService.GetMarketplaceFeePercent(deal.Marketplace);

        deal.Pricing ??= new PricingConfig();
        deal.Pricing.PublicPricePerYear = listPrice;
        deal.Pricing.MarketplaceFeePercent = feePercent;
        pricingService.Calculate(deal.Pricing);
        deal.ExpectedValue = deal.Pricing.NetContractValue;

        deal.StepNumber = 2;
        deal.Stage = "Product Selection";
        deal.LastUpdated = "Just now";
        history.LogProducts(deal, productIds.Count);
        audit.LogDeal(deal, "Products", "Products selected",
            $"{productIds.Count} product(s) selected" + (productIds.Count > 0 ? $": {string.Join(", ", productIds)}." : "."));
        offerRequests.MarkEngagementChanged(deal, $"Products changed — {productIds.Count} selected.");
        approvals.HandleProductsChange(deal);
        store.SaveDeals();
        return dealService.ToDetail(deal);
    }

    [HttpPost("{id}/pricing/preview")]
    public ActionResult<object> PreviewPricing(string id, [FromBody] PricingConfig pricing)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();

        if (pricing.PublicPricePerYear <= 0)
            pricing.PublicPricePerYear = dealService.GetListPriceFromProducts(deal);
        if (pricing.MarketplaceFeePercent <= 0)
            pricing.MarketplaceFeePercent = PricingService.GetMarketplaceFeePercent(deal.Marketplace);

        pricingService.Calculate(pricing);
        return new
        {
            pricing,
            insight = pricingService.BuildInsight(pricing.DiscountPercent, pricing.NetContractValue),
            selectedProducts = dealService.GetSelectedProducts(deal)
        };
    }

    [HttpPost("{id}/pricing")]
    public ActionResult<DealDetailDto> SetPricing(string id, [FromBody] PricingConfig pricing)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();
        if (deal.Locked) return BadRequest(new { success = false, message = LockedMessage });

        if (pricing.PublicPricePerYear <= 0)
            pricing.PublicPricePerYear = dealService.GetListPriceFromProducts(deal);
        if (pricing.MarketplaceFeePercent <= 0)
            pricing.MarketplaceFeePercent = PricingService.GetMarketplaceFeePercent(deal.Marketplace);

        pricingService.Calculate(pricing);
        deal.Pricing = pricing;
        deal.ExpectedValue = pricing.NetContractValue;
        deal.StepNumber = 3;
        deal.Stage = "Pricing";
        deal.LastUpdated = "Just now";
        history.LogPricing(deal);
        audit.LogDeal(deal, "Pricing", "Pricing configured",
            $"Net ${deal.Pricing?.NetContractValue:N0}, {deal.Pricing?.DiscountPercent}% discount, {deal.Pricing?.DurationValue} {deal.Pricing?.DurationType}.");
        offerRequests.MarkEngagementChanged(deal, $"Pricing changed — net ${deal.Pricing?.NetContractValue:N0}, {deal.Pricing?.DiscountPercent}% discount.");
        approvals.HandlePricingChange(deal);
        store.SaveDeals();
        return dealService.ToDetail(deal);
    }

    [HttpPost("{id}/meeting-notes")]
    public ActionResult<object> SetMeetingNotes(string id, [FromBody] SaveMeetingNotesRequest request, [FromServices] IAiService ai)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();

        var sessionsAdded = meetingNotes.ApplySave(deal, request);
        history.LogMeetingNotes(deal,
            sessionsAdded,
            deal.MeetingNotes?.ActionItems?.Count ?? 0,
            deal.MeetingNotes?.Reminders?.Count ?? 0);
        audit.LogDeal(deal, "Meeting Notes", sessionsAdded > 0 ? "Meeting summary added" : "Meeting notes updated",
            $"{deal.MeetingNotes?.ActionItems?.Count ?? 0} action item(s), {deal.MeetingNotes?.Reminders?.Count ?? 0} reminder(s).");

        deal.StepNumber = 4;
        deal.Stage = "Meeting Notes";
        deal.LastUpdated = "Just now";
        store.SaveDeals();

        var latestNotes = deal.MeetingNotes?.Sessions?.FirstOrDefault()?.RawNotes ?? deal.MeetingNotes?.RawNotes ?? "";
        var latestExtracted = deal.MeetingNotes?.Sessions?.FirstOrDefault()?.Extracted ?? deal.MeetingNotes?.Extracted;

        return new
        {
            deal = dealService.ToDetail(deal),
            insight = ai.BuildInsight(latestNotes, latestExtracted ?? new AiExtractedSummary())
        };
    }

    [HttpGet("{id}/approvals")]
    public ActionResult<object> GetApprovals(string id)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();
        meetingNotes.Normalize(deal);
        var summary = approvals.BuildSummary(deal);
        store.SaveDeals();
        return new { deal = dealService.ToDetail(deal), approvals = summary };
    }

    [HttpPost("{id}/approvals/action")]
    public ActionResult<object> ApprovalAction(string id, [FromBody] ApprovalActionRequest request)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();

        var result = approvals.ApplyAction(deal, request);
        if (!result.Success) return BadRequest(new { success = false, message = result.Message });

        deal.LastUpdated = "Just now";
        store.SaveDeals();
        return new { success = true, message = result.Message, approvals = approvals.BuildSummary(deal) };
    }

    [HttpPost("{id}/approvals/regenerate-documents")]
    public ActionResult<object> RegenerateDocuments(string id)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();

        approvals.RegenerateDocuments(deal);
        deal.LastUpdated = "Just now";
        store.SaveDeals();
        return new { success = true, approvals = approvals.BuildSummary(deal) };
    }

    [HttpPost("{id}/approvals/enter")]
    public ActionResult<object> EnterApprovals(string id)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();

        approvals.EnsurePlan(deal);
        deal.StepNumber = 5;
        deal.Stage = "Approval";
        deal.LastUpdated = "Just now";
        history.Log(deal, "Approvals", "Deal entered approval workflow.", approvals.BuildSummary(deal).NextStep);
        audit.LogDeal(deal, "Approvals", "Entered approval workflow", $"Next step: {approvals.BuildSummary(deal).NextStep}.");
        store.SaveDeals();
        return new { deal = dealService.ToDetail(deal), approvals = approvals.BuildSummary(deal) };
    }

    [HttpPost("{id}/approvals/submit")]
    public ActionResult<object> SubmitApprovals(string id)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();

        if (!approvals.CanProceed(deal))
            return BadRequest(new { success = false, message = "All required approvals must be approved before proceeding." });

        deal.MarketplaceStatus = "In Review";
        deal.LastUpdated = "Just now";
        history.Log(deal, "Approvals", "Deal submitted for offer request.", "All approvals complete.");
        audit.LogDeal(deal, "Approvals", "Submitted for offer request", "All approvals complete — status set to In Review.");
        store.SaveDeals();
        var offer = offerRequests.Record(deal);
        return new { success = true, message = "Deal submitted. Proceed to offer request summary.", offerRequestId = offer.Id, deal = dealService.ToDetail(deal) };
    }

    /// <summary>
    /// Final submit for an engagement. The resulting status depends on the engagement type:
    /// Private Offer / Free Trial → "In Review"; Workshop / Hackathon / POC → "Completed";
    /// Summit/Event Lead / Internal Sales Activity / External Source Lead → "Lead".
    /// </summary>
    [HttpPost("{id}/submit-engagement")]
    public ActionResult<object> SubmitEngagement(string id)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();

        var (status, message) = ResolveSubmitOutcome(deal.EngagementType);
        deal.MarketplaceStatus = status;
        deal.LastUpdated = "Just now";
        history.Log(deal, "Engagement", $"Engagement submitted — {message}", $"Status set to {status}.");
        audit.LogDeal(deal, "Engagement", "Engagement submitted", $"{message} Status set to {status}.");
        store.SaveDeals();
        var offer = offerRequests.Record(deal);
        return new { success = true, message, status, offerRequestId = offer.Id, deal = dealService.ToDetail(deal) };
    }

    private static (string status, string message) ResolveSubmitOutcome(string engagementType) => engagementType switch
    {
        "Private Offer" or "Free Trial" => ("In Review", "Submitted to SaaSify."),
        "Workshop" or "Hackathon" or "POC" => ("Completed", "Marked completed."),
        "Summit/Event Lead" or "Internal Sales Activity" or "External Source Lead" => ("Lead", "Saved — convert later."),
        _ => ("In Review", "Submitted.")
    };

    [HttpGet("{id}/approvals/documents/{docId}")]
    public ActionResult GetApprovalDocument(string id, string docId)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();
        meetingNotes.Normalize(deal);
        approvals.EnsurePlan(deal);
        var doc = approvals.GetDocumentHtml(deal, docId);
        if (doc.Contains("Document not found", StringComparison.Ordinal))
            return NotFound();
        approvals.LogDocumentView(deal, docId);
        store.SaveDeals();
        return Content(doc, "text/html; charset=utf-8");
    }

    [HttpGet("{id}/approvals/documents/{docId}/download")]
    public ActionResult DownloadApprovalDocument(string id, string docId)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();
        meetingNotes.Normalize(deal);
        approvals.EnsurePlan(deal);
        var docEntity = deal.Approvals?.Documents.FirstOrDefault(d => d.Id == docId);
        if (docEntity is null) return NotFound();
        var html = approvals.GetDocumentHtml(deal, docId);
        approvals.LogDocumentDownload(deal, docId);
        store.SaveDeals();
        var fileName = string.IsNullOrWhiteSpace(docEntity.FileName) ? $"{docId}.html" : docEntity.FileName;
        return File(System.Text.Encoding.UTF8.GetBytes(html), "text/html", fileName);
    }

    [HttpPost("{id}/approvals/unlock")]
    public ActionResult<object> UnlockApprovals(string id)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();
        if (!approvals.UnlockForEdits(deal))
            return BadRequest(new { success = false, message = "Documents cannot be unlocked for editing." });
        deal.LastUpdated = "Just now";
        store.SaveDeals();
        return new { success = true, approvals = approvals.BuildSummary(deal) };
    }

    // Private offers often aren't tied to a scheduled event/campaign, so the tag is optional for them too.
    private static readonly string[] TagOptionalTypes = ["Private Offer", "Internal Sales Activity", "External Source Lead"];

    // Lead-capture / internal types carry no marketplace offer, so a marketplace isn't required.
    private static readonly string[] MarketplaceOptionalTypes = ["Internal Sales Activity", "External Source Lead"];

    /// <summary>
    /// When an engagement is tagged to a campaign / event, its marketplace is derived from that
    /// event's marketplace (AWS / Azure / GCP) so the two can never diverge. Falls back to the
    /// supplied value when there is no tag or the tag carries no marketplace.
    /// </summary>
    private string DeriveMarketplace(string campaignEventId, string fallback)
    {
        if (string.IsNullOrWhiteSpace(campaignEventId)) return fallback;
        var ev = store.CampaignEvents.FirstOrDefault(e => e.Id == campaignEventId);
        return ev is not null && !string.IsNullOrWhiteSpace(ev.Marketplace) ? ev.Marketplace : fallback;
    }

    private static string? ValidateDeal(CreateDealRequest r)
    {
        if (string.IsNullOrWhiteSpace(r.EngagementType)) return "Engagement type is required.";
        if (string.IsNullOrWhiteSpace(r.ContactEmail)) return "Work email is required.";
        if (!IsValidEmail(r.ContactEmail)) return "Work email must be a valid email address.";
        if (!MarketplaceOptionalTypes.Contains(r.EngagementType.Trim()) && string.IsNullOrWhiteSpace(r.Marketplace))
            return "At least one marketplace is required.";
        // Campaign / event tag is required except for internal/external lead types.
        if (!TagOptionalTypes.Contains(r.EngagementType.Trim()) && string.IsNullOrWhiteSpace(r.CampaignEventId))
            return "Campaign / event tag is required for this engagement type.";
        if (string.IsNullOrWhiteSpace(r.Owner)) return "Deal owner is required.";
        // Company name + engagement name are auto-derived from the work email when omitted.
        return null;
    }

    /// <summary>Keep only non-empty, trimmed account IDs (one per marketplace).</summary>
    private static Dictionary<string, string> NormalizeAccountIds(Dictionary<string, string>? ids)
    {
        var result = new Dictionary<string, string>();
        if (ids is null) return result;
        foreach (var (key, value) in ids)
        {
            if (!string.IsNullOrWhiteSpace(value))
                result[key.Trim()] = value.Trim();
        }
        return result;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email.Trim());
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }

    private static Deal CloneDeal(Deal d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Customer = d.Customer,
        ContactName = d.ContactName,
        ContactEmail = d.ContactEmail,
        Phone = d.Phone,
        Priority = d.Priority,
        Location = d.Location,
        Industry = d.Industry,
        Marketplace = d.Marketplace,
        DealType = d.DealType,
        EngagementType = d.EngagementType,
        QuickCapture = d.QuickCapture,
        CampaignEventId = d.CampaignEventId,
        CampaignEventName = d.CampaignEventName,
        BillingAccountId = d.BillingAccountId,
        BillingAccountIds = d.BillingAccountIds,
        ExpectedValue = d.ExpectedValue,
        ExpectedCloseDate = d.ExpectedCloseDate,
        Description = d.Description,
        Stage = d.Stage,
        StepNumber = d.StepNumber,
        MarketplaceStatus = d.MarketplaceStatus,
        Owner = d.Owner,
        CreatedAt = d.CreatedAt
    };
}
