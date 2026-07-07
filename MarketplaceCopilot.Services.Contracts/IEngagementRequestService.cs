using MarketplaceCopilot.Entities;

namespace MarketplaceCopilot.Services.Contracts;

/// <summary>Engagement requests submitted by customers from the Customer Portal.</summary>
public interface IEngagementRequestService
{
    List<EngagementRequest> GetAllForCustomer(string customerEmail);
    EngagementRequest? GetById(string id);
    EngagementRequest Create(CreateEngagementRequestDto dto, string customerEmail, string customerName, string companyName);
}
