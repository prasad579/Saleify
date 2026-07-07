using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

/// <summary>Engagement requests submitted by customers from the Customer Portal.</summary>
[ApiController]
[Route("api/engagement-requests")]
public class EngagementRequestsController(IEngagementRequestService requests) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<EngagementRequest>> GetAll([FromQuery] string customerEmail)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
            return BadRequest(new { message = "customerEmail is required." });
        return Ok(requests.GetAllForCustomer(customerEmail));
    }

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
}
