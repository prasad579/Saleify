using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

/// <summary>Engagement requests submitted by customers from the Customer Portal.</summary>
[ApiController]
[Route("api/engagement-requests")]
public class EngagementRequestsController(IEngagementRequestService requests, ITenantAccessor tenant) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<EngagementRequest>> GetAll([FromQuery] string customerEmail)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
            return BadRequest(new { message = "customerEmail is required." });
        return Ok(requests.GetAllForCustomer(customerEmail));
    }

    /// <summary>All requests across all customers — the internal/staff view.</summary>
    [HttpGet("all")]
    public ActionResult<IEnumerable<EngagementRequest>> GetAllForStaff() => Ok(requests.GetAll());

    [HttpGet("{id}")]
    public ActionResult<EngagementRequest> GetById(string id)
    {
        var request = requests.GetById(id);
        return request is null ? NotFound() : request;
    }

    [HttpPost]
    public ActionResult<EngagementRequest> Create([FromBody] CreateEngagementRequestDto dto, [FromQuery] string customerEmail, [FromQuery] string customerName, [FromQuery] string companyName)
    {
        if (dto is null || string.IsNullOrWhiteSpace(dto.RequestType))
            return BadRequest(new { message = "A request type is required." });
        if (string.IsNullOrWhiteSpace(customerEmail))
            return BadRequest(new { message = "customerEmail is required." });

        var created = requests.Create(dto, customerEmail, customerName, companyName);
        return Ok(created);
    }

    /// <summary>Create a Deal from a submitted request; the request is marked Converted and locked from re-conversion.</summary>
    [HttpPost("{id}/convert-to-deal")]
    public ActionResult<object> ConvertToDeal(string id, [FromQuery] string owner)
    {
        var request = requests.GetById(id);
        if (request is null) return NotFound();
        if (request.Status == "Converted")
            return BadRequest(new { success = false, message = $"Already converted to {request.ConvertedDealId}." });

        var deal = requests.ConvertToDeal(id, owner, tenant.TenantId);
        if (deal is null) return BadRequest(new { success = false, message = "Could not convert this request." });

        return Ok(new { success = true, deal });
    }
}
