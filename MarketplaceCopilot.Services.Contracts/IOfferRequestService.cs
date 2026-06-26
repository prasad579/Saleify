using MarketplaceCopilot.Entities;

namespace MarketplaceCopilot.Services.Contracts;

/// <summary>
/// Tracks engagements that have been submitted ("pushed") to a destination and the JSON payload
/// sent, plus any response captured back. Backs the Offer Requests page.
/// </summary>
public interface IOfferRequestService
{
    /// <summary>All offer requests (newest first), backfilling any already-submitted engagements.</summary>
    List<OfferRequest> GetAll();

    /// <summary>A single offer request by id (after backfill), or null if not found.</summary>
    OfferRequest? GetById(string id);

    /// <summary>Create or update the offer request for a freshly-submitted engagement.</summary>
    OfferRequest Record(Deal deal, string? destination = null);

    /// <summary>Whether the engagement has already been pushed as an offer request.</summary>
    bool HasOfferRequest(string dealId);

    /// <summary>
    /// Flag that the engagement was edited after submission, so its offer request is out of date.
    /// No-op when the engagement has no offer request.
    /// </summary>
    void MarkEngagementChanged(Deal deal, string summary);

    /// <summary>Capture the destination's response to an offer request. Returns null if not found.</summary>
    OfferRequest? CaptureResponse(string id, CaptureResponseRequest request);
}
