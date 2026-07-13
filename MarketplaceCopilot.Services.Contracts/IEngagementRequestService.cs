using MarketplaceCopilot.Entities;

namespace MarketplaceCopilot.Services.Contracts;

/// <summary>Engagement requests submitted by customers from the Customer Portal.</summary>
public interface IEngagementRequestService
{
    List<EngagementRequest> GetAllForCustomer(string customerEmail);
    /// <summary>All requests across all customers, newest first — the internal/staff view.</summary>
    List<EngagementRequest> GetAll();
    EngagementRequest? GetById(string id);
    EngagementRequest Create(CreateEngagementRequestDto dto, string customerEmail, string customerName, string companyName);
    /// <summary>Create a Deal from a submitted request and mark the request Converted. Null if already converted or not found.</summary>
    Deal? ConvertToDeal(string requestId, string owner, string tenantId);
}
