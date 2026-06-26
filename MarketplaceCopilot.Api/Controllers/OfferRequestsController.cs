using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

/// <summary>
/// Offer Requests — the list of engagements pushed to a destination (SaaSify / marketplace), the
/// exact JSON payload sent for each, and the response captured back.
/// </summary>
[ApiController]
[Route("api/offer-requests")]
public class OfferRequestsController(IOfferRequestService offers) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<OfferRequest>> GetAll() => offers.GetAll();

    [HttpGet("{id}")]
    public ActionResult<OfferRequest> GetById(string id)
    {
        var offer = offers.GetById(id);
        return offer is null ? NotFound() : offer;
    }

    /// <summary>Capture the destination's response to an offer request.</summary>
    [HttpPost("{id}/response")]
    public ActionResult<OfferRequest> CaptureResponse(string id, [FromBody] CaptureResponseRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new { message = "A response status is required." });

        var updated = offers.CaptureResponse(id, request);
        return updated is null ? NotFound() : Ok(updated);
    }
}
