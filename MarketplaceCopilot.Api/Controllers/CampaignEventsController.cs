using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

[ApiController]
[Route("api/campaign-events")]
public class CampaignEventsController(DataStore store, IAuditService audit, ITenantAccessor tenant) : ControllerBase
{
    /// <summary>Engagement types used for the conversion funnel, in funnel order.</summary>
    private static readonly string[] EngagementOrder =
        ["Workshop", "Hackathon", "Private Offer"];

    [HttpGet]
    public ActionResult<IEnumerable<CampaignEvent>> GetAll() => store.CampaignEvents;

    [HttpGet("{id}")]
    public ActionResult<CampaignEvent> GetById(string id)
    {
        var ev = store.CampaignEvents.FirstOrDefault(e => e.Id == id);
        return ev is null ? NotFound() : ev;
    }

    [HttpPost]
    public ActionResult<CampaignEvent> Create([FromBody] CampaignEvent request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Event name is required." });
        if (string.IsNullOrWhiteSpace(request.StartDate) || string.IsNullOrWhiteSpace(request.EndDate))
            return BadRequest(new { message = "Start and end dates are required." });

        var ev = new CampaignEvent
        {
            Id = store.NextCampaignEventId(),
            Name = request.Name.Trim(),
            Marketplace = request.Marketplace?.Trim() ?? "",
            StartDate = request.StartDate.Trim(),
            EndDate = request.EndDate.Trim(),
            Description = request.Description?.Trim() ?? "",
            Paused = request.Paused,
            CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd")
        };
        store.CampaignEvents.Insert(0, ev);
        store.SaveCampaignEvents();
        audit.Log("Settings", "Campaign / event tag created",
            $"{ev.Name}{(string.IsNullOrWhiteSpace(ev.Marketplace) ? "" : $" ({ev.Marketplace})")}, {ev.StartDate} → {ev.EndDate}.",
            "Campaign / Event Tags", ev.Id);
        return Ok(ev);
    }

    [HttpPut("{id}")]
    public ActionResult<CampaignEvent> Update(string id, [FromBody] CampaignEvent request)
    {
        var ev = store.CampaignEvents.FirstOrDefault(e => e.Id == id);
        if (ev is null) return NotFound();

        ev.Name = request.Name?.Trim() ?? ev.Name;
        ev.Marketplace = request.Marketplace?.Trim() ?? ev.Marketplace;
        ev.StartDate = request.StartDate?.Trim() ?? ev.StartDate;
        ev.EndDate = request.EndDate?.Trim() ?? ev.EndDate;
        ev.Description = request.Description?.Trim() ?? ev.Description;
        ev.Paused = request.Paused;
        store.SaveCampaignEvents();
        audit.Log("Settings", "Campaign / event tag updated", $"{ev.Name} updated.", "Campaign / Event Tags", ev.Id);
        return ev;
    }

    /// <summary>Pause/resume a tag. Paused tags are hidden from the engagement-creation dropdown.</summary>
    [HttpPost("{id}/toggle-pause")]
    public ActionResult<CampaignEvent> TogglePause(string id)
    {
        var ev = store.CampaignEvents.FirstOrDefault(e => e.Id == id);
        if (ev is null) return NotFound();
        ev.Paused = !ev.Paused;
        store.SaveCampaignEvents();
        audit.Log("Settings", ev.Paused ? "Campaign / event tag paused" : "Campaign / event tag resumed", ev.Name,
            "Campaign / Event Tags", ev.Id);
        return ev;
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(string id)
    {
        var ev = store.CampaignEvents.FirstOrDefault(e => e.Id == id);
        if (ev is null) return NotFound();
        store.CampaignEvents.Remove(ev);
        store.SaveCampaignEvents();
        audit.Log("Settings", "Campaign / event tag deleted", ev.Name, "Campaign / Event Tags", ev.Id);
        return Ok(new { success = true });
    }

    [HttpGet("{id}/conversion")]
    public ActionResult<ConversionFunnel> GetConversion(string id)
    {
        var ev = store.CampaignEvents.FirstOrDefault(e => e.Id == id);
        if (ev is null) return NotFound();

        var deals = store.Deals.Where(d => d.CampaignEventId == id && d.TenantId == tenant.TenantId).ToList();
        var funnel = new ConversionFunnel
        {
            EventId = ev.Id,
            EventName = ev.Name,
            TotalDeals = deals.Count,
            ClosedWon = deals.Count(d => d.MarketplaceStatus == "Published"),
            Stages = [new FunnelStage { Label = "Deals", Count = deals.Count }]
        };

        foreach (var type in EngagementOrder)
        {
            funnel.Stages.Add(new FunnelStage
            {
                Label = type + "s",
                Count = deals.Count(d => string.Equals(d.EngagementType, type, StringComparison.OrdinalIgnoreCase))
            });
        }

        funnel.Stages.Add(new FunnelStage { Label = "Closed Won", Count = funnel.ClosedWon });
        return funnel;
    }
}
