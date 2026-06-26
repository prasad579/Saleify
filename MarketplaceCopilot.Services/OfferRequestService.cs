using System.Text.Json;
using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;

namespace MarketplaceCopilot.Services;

public class OfferRequestService(DataStore store, IDealService dealService, IActingUserAccessor actingUser, IAuditService audit) : IOfferRequestService
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>Engagement statuses that indicate the engagement has been submitted to a destination.</summary>
    private static readonly string[] SubmittedStatuses = ["In Review", "Published", "Completed", "Accepted", "Rejected"];

    public List<OfferRequest> GetAll()
    {
        Backfill();
        return store.OfferRequests
            .OrderByDescending(o => o.SubmittedAt)
            .ToList();
    }

    public OfferRequest? GetById(string id)
    {
        Backfill();
        return store.OfferRequests.FirstOrDefault(o => o.Id == id);
    }

    /// <summary>
    /// Create offer requests for any already-submitted engagements that aren't tracked yet, and
    /// reconcile the lock state: an engagement that has an in-sync offer request should be locked.
    /// </summary>
    private void Backfill()
    {
        var offersChanged = false;
        var dealsChanged = false;
        foreach (var deal in store.Deals.Where(d => SubmittedStatuses.Contains(d.MarketplaceStatus)))
        {
            var request = store.OfferRequests.FirstOrDefault(o => o.DealId == deal.Id);
            if (request is null)
            {
                UpsertInternal(deal, destination: null, persist: false);
                offersChanged = true;
                dealsChanged = true;
            }
            else if (!request.ChangedSinceSubmission && !deal.Locked)
            {
                // Offer request is in sync with the engagement → it should be locked from edits.
                deal.Locked = true;
                dealsChanged = true;
            }
        }
        if (offersChanged) store.SaveOfferRequests();
        if (dealsChanged) store.SaveDeals();
    }

    public OfferRequest Record(Deal deal, string? destination = null)
        => UpsertInternal(deal, destination, persist: true);

    public bool HasOfferRequest(string dealId)
        => store.OfferRequests.Any(o => o.DealId == dealId);

    public void MarkEngagementChanged(Deal deal, string summary)
    {
        var request = store.OfferRequests.FirstOrDefault(o => o.DealId == deal.Id);
        if (request is null) return;

        request.ChangedSinceSubmission = true;
        request.LastChangeSummary = summary;
        deal.ChangedSinceSubmission = true;
        store.SaveOfferRequests();
        audit.LogDeal(deal, "Offer Request", "Engagement changed after submission",
            $"{request.Id} is now out of date — {summary} Re-submit to push the changes.");
    }

    private OfferRequest UpsertInternal(Deal deal, string? destination, bool persist)
    {
        var existing = store.OfferRequests.FirstOrDefault(o => o.DealId == deal.Id);
        var products = dealService.GetSelectedProducts(deal);
        var user = actingUser.CurrentUser;
        var now = Now();

        var request = existing ?? new OfferRequest
        {
            Id = store.NextOfferRequestId(),
            DealId = deal.Id,
            SubmittedAt = now,
            SubmittedBy = user
        };

        request.EngagementName = string.IsNullOrWhiteSpace(deal.Name) ? deal.Id : deal.Name;
        request.Customer = deal.Customer;
        request.EngagementType = deal.EngagementType;
        request.Marketplace = deal.Marketplace;
        request.Products = products.Select(p => p.Name).ToList();
        request.Destination = string.IsNullOrWhiteSpace(destination) ? "SaaSify" : destination;
        request.Status = deal.MarketplaceStatus;
        request.Value = deal.Pricing?.NetContractValue is > 0 ? deal.Pricing!.NetContractValue : deal.ExpectedValue;
        request.RequestJson = BuildRequestJson(deal, request, products);
        // Pushing (or re-pushing) syncs the offer request with the engagement and locks editing.
        request.ChangedSinceSubmission = false;
        request.LastChangeSummary = "";
        deal.Locked = true;
        deal.ChangedSinceSubmission = false;

        if (existing is null)
        {
            store.OfferRequests.Insert(0, request);
            audit.LogDeal(deal, "Offer Request", "Offer request pushed",
                $"Pushed to {request.Destination}{(string.IsNullOrWhiteSpace(request.Marketplace) ? "" : $" ({request.Marketplace})")} as {request.Id}. Engagement locked from edits.");
        }
        else
        {
            // Re-submission of an already-tracked engagement — refresh the payload + timestamp.
            request.SubmittedAt = now;
            request.SubmittedBy = user;
            audit.LogDeal(deal, "Offer Request", "Offer request re-pushed", $"{request.Id} payload refreshed and re-synced. Engagement re-locked.");
        }

        if (persist) store.SaveOfferRequests();
        return request;
    }

    public OfferRequest? CaptureResponse(string id, CaptureResponseRequest req)
    {
        var request = store.OfferRequests.FirstOrDefault(o => o.Id == id);
        if (request is null) return null;

        var user = string.IsNullOrWhiteSpace(req.User) ? actingUser.CurrentUser : req.User.Trim();
        request.ResponseReceived = true;
        request.ResponseStatus = string.IsNullOrWhiteSpace(req.Status) ? "Pending" : req.Status.Trim();
        request.ResponseReference = req.Reference?.Trim() ?? "";
        request.ResponseNotes = req.Notes?.Trim() ?? "";
        request.ResponseJson = string.IsNullOrWhiteSpace(req.Json) ? BuildResponseJson(request) : req.Json.Trim();
        request.ResponseAt = Now();
        request.ResponseBy = user;

        // Reflect an accepted/rejected response on the underlying engagement status.
        var deal = store.Deals.FirstOrDefault(d => d.Id == request.DealId);
        if (deal is not null)
        {
            if (request.ResponseStatus.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
            {
                deal.MarketplaceStatus = "Published";
                request.Status = "Published";
                deal.LastUpdated = "Just now";
                store.SaveDeals();
            }
            else if (request.ResponseStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase))
            {
                deal.MarketplaceStatus = "Rejected";
                request.Status = "Rejected";
                deal.LastUpdated = "Just now";
                store.SaveDeals();
            }
            audit.LogDeal(deal, "Offer Request", $"Response captured — {request.ResponseStatus}",
                string.IsNullOrWhiteSpace(request.ResponseReference) ? request.ResponseNotes : $"Ref {request.ResponseReference}. {request.ResponseNotes}", user);
        }

        store.SaveOfferRequests();
        return request;
    }

    private string BuildRequestJson(Deal deal, OfferRequest request, List<Product> products)
    {
        var p = deal.Pricing;
        var payload = new
        {
            offerRequestId = request.Id,
            engagementId = deal.Id,
            engagementType = deal.EngagementType,
            destination = request.Destination,
            submittedAt = request.SubmittedAt,
            submittedBy = request.SubmittedBy,
            status = deal.MarketplaceStatus,
            customer = new
            {
                name = deal.Customer,
                contactName = deal.ContactName,
                contactEmail = deal.ContactEmail,
                industry = deal.Industry,
                location = deal.Location
            },
            marketplace = deal.Marketplace,
            billingAccountIds = deal.BillingAccountIds,
            campaignEvent = string.IsNullOrWhiteSpace(deal.CampaignEventId)
                ? null
                : new { id = deal.CampaignEventId, name = deal.CampaignEventName },
            products = products.Select(pr => new
            {
                id = pr.Id,
                name = pr.Name,
                listPricePerYear = pr.ListPricePerYear,
                billingModel = pr.BillingModel
            }),
            pricing = p is null ? null : new
            {
                offerType = p.OfferType,
                durationMonths = p.DurationMonths,
                discountPercent = p.DiscountPercent,
                publicContractValue = p.PublicContractValue,
                netContractValue = p.NetContractValue,
                marketplaceFee = p.MarketplaceFee,
                totalPayable = p.TotalPayable,
                flexiblePayments = p.FlexiblePaymentsEnabled,
                installments = p.InstallmentCount
            },
            owner = deal.Owner
        };
        return JsonSerializer.Serialize(payload, PayloadJsonOptions);
    }

    private static string BuildResponseJson(OfferRequest request) =>
        JsonSerializer.Serialize(new
        {
            offerRequestId = request.Id,
            engagementId = request.DealId,
            responseStatus = request.ResponseStatus,
            reference = request.ResponseReference,
            notes = request.ResponseNotes,
            receivedAt = request.ResponseAt,
            receivedBy = request.ResponseBy
        }, PayloadJsonOptions);

    private static string Now() => DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
}
