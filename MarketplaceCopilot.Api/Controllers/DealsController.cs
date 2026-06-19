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
    IApprovalService approvals) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<Deal>> GetAll()
    {
        foreach (var d in store.Deals)
            meetingNotes.Normalize(d);
        return store.Deals;
    }

    [HttpGet("stats")]
    public ActionResult<DealStats> GetStats()
    {
        var deals = store.Deals;
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

    [HttpPost]
    public ActionResult<CreateDealResponse> Create([FromBody] CreateDealRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Customer))
            return BadRequest(new CreateDealResponse { Success = false, Message = "Customer is required." });
        if (string.IsNullOrWhiteSpace(request.Marketplace))
            return BadRequest(new CreateDealResponse { Success = false, Message = "Marketplace is required." });
        if (string.IsNullOrWhiteSpace(request.ContactName))
            return BadRequest(new CreateDealResponse { Success = false, Message = "Primary contact is required." });
        if (string.IsNullOrWhiteSpace(request.ContactEmail))
            return BadRequest(new CreateDealResponse { Success = false, Message = "Contact email is required." });

        var deal = new Deal
        {
            Id = store.NextDealId(),
            Name = string.IsNullOrWhiteSpace(request.Name) ? $"{request.Customer} Deal" : request.Name.Trim(),
            Customer = request.Customer.Trim(),
            ContactName = request.ContactName.Trim(),
            ContactEmail = request.ContactEmail.Trim(),
            Location = request.Location?.Trim() ?? "",
            Industry = request.Industry?.Trim() ?? "",
            Marketplace = request.Marketplace.Trim(),
            DealType = string.IsNullOrWhiteSpace(request.DealType) ? "New Deal" : request.DealType.Trim(),
            ExpectedValue = request.ExpectedValue,
            ExpectedCloseDate = request.ExpectedCloseDate?.Trim() ?? "",
            Description = request.Description?.Trim() ?? "",
            Stage = "Draft",
            StepNumber = 1,
            TotalSteps = 5,
            MarketplaceStatus = "Draft",
            LastUpdated = "Just now",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Owner = "Srinivas K"
        };

        history.LogCreated(deal);
        store.Deals.Insert(0, deal);
        store.SaveDeals();

        return Ok(new CreateDealResponse
        {
            Success = true,
            Message = $"Deal {deal.Id} created successfully.",
            Deal = deal
        });
    }

    [HttpPut("{id}")]
    public ActionResult<Deal> Update(string id, [FromBody] Deal updated)
    {
        var index = store.Deals.FindIndex(d => d.Id == id);
        if (index < 0) return NotFound();

        var before = CloneDeal(store.Deals[index]);
        updated.Id = id;
        updated.LastUpdated = "Just now";
        updated.ChangeHistory = store.Deals[index].ChangeHistory;
        updated.MeetingNotes = store.Deals[index].MeetingNotes;
        updated.Approvals = store.Deals[index].Approvals;
        updated.CreatedAt = store.Deals[index].CreatedAt;
        history.LogDealUpdated(before, updated);
        store.Deals[index] = updated;
        store.SaveDeals();
        return updated;
    }

    [HttpPost("{id}/products")]
    public ActionResult<DealDetailDto> SetProducts(string id, [FromBody] List<string> productIds)
    {
        var deal = store.Deals.FirstOrDefault(d => d.Id == id);
        if (deal is null) return NotFound();

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
        store.SaveDeals();
        return new { success = true, message = "Deal submitted. Proceed to offer request summary.", deal = dealService.ToDetail(deal) };
    }

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

    private static Deal CloneDeal(Deal d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Customer = d.Customer,
        ContactName = d.ContactName,
        ContactEmail = d.ContactEmail,
        Location = d.Location,
        Industry = d.Industry,
        Marketplace = d.Marketplace,
        DealType = d.DealType,
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
