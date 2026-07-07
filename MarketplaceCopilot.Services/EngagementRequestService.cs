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
}
