using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;

namespace MarketplaceCopilot.Services;

public class EngagementRequestService(DataStore store, IAuditService audit) : IEngagementRequestService
{
    public List<EngagementRequest> GetAllForCustomer(string customerEmail) =>
        store.EngagementRequests
            .Where(r => r.CustomerEmail.Equals(customerEmail, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

    public List<EngagementRequest> GetAll() =>
        store.EngagementRequests.OrderByDescending(r => r.CreatedAt).ToList();

    public EngagementRequest? GetById(string id) =>
        store.EngagementRequests.FirstOrDefault(r => r.Id == id);

    public EngagementRequest Create(CreateEngagementRequestDto dto, string customerEmail, string customerName, string companyName)
    {
        var request = new EngagementRequest
        {
            Id = store.NextEngagementRequestId(),
            RequestType = dto.RequestType,
            Marketplace = dto.Marketplace,
            ProductIds = dto.ProductIds ?? [],
            ExpectedStartDate = dto.ExpectedStartDate,
            ExpectedDuration = dto.ExpectedDuration,
            EstimatedUsers = dto.EstimatedUsers,
            BusinessNeed = dto.BusinessNeed,
            BudgetRange = dto.BudgetRange,
            OtherRequirements = dto.OtherRequirements,
            ContactPreference = dto.ContactPreference,
            PreferredTimeToContact = dto.PreferredTimeToContact,
            AttachmentNames = dto.AttachmentNames ?? [],
            Status = "Submitted",
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            CompanyName = companyName,
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC"
        };

        store.EngagementRequests.Insert(0, request);
        store.SaveEngagementRequests();

        audit.Log("Engagement Request", "Request submitted",
            $"{request.RequestType} request submitted by {customerName} ({companyName}).",
            entity: "EngagementRequest", entityId: request.Id, user: customerName);

        return request;
    }

    public Deal? ConvertToDeal(string requestId, string owner, string tenantId)
    {
        var request = GetById(requestId);
        if (request is null || request.Status == "Converted") return null;

        var customer = string.IsNullOrWhiteSpace(request.CompanyName) ? request.CustomerName : request.CompanyName;
        var deal = new Deal
        {
            Id = store.NextDealId(),
            TenantId = tenantId,
            Name = string.Join(" — ", new[] { request.RequestType, customer }.Where(s => !string.IsNullOrWhiteSpace(s))),
            Customer = customer,
            ContactName = request.CustomerName,
            ContactEmail = request.CustomerEmail,
            Marketplace = request.Marketplace,
            EngagementType = request.RequestType,
            ProductIds = request.ProductIds ?? [],
            ExpectedCloseDate = request.ExpectedStartDate,
            Description = request.BusinessNeed,
            Stage = "Draft",
            StepNumber = 1,
            TotalSteps = 5,
            MarketplaceStatus = "Draft",
            Owner = string.IsNullOrWhiteSpace(owner) ? "Srinivas K" : owner.Trim(),
            LastUpdated = "Just now",
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            RequestedByCustomer = true,
            EngagementRequestId = request.Id
        };

        store.Deals.Insert(0, deal);
        store.SaveDeals();

        request.Status = "Converted";
        request.ConvertedDealId = deal.Id;
        store.SaveEngagementRequests();

        audit.LogDeal(deal, "Engagement", "Created from customer request",
            $"Converted from engagement request {request.Id} submitted by {request.CustomerName} ({request.CompanyName}).");

        return deal;
    }
}
