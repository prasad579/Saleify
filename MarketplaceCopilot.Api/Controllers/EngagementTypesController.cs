using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

/// <summary>
/// Read/update the configurable engagement-type catalog (Settings → Engagement Types) that drives
/// which types are offered and which sections (Products / Pricing / Meeting Notes / Approvals) apply
/// to each one — e.g. turning approvals off for a Free Trial.
/// </summary>
[ApiController]
[Route("api/engagement-types")]
public class EngagementTypesController(DataStore store, IAuditService audit) : ControllerBase
{
    [HttpGet]
    public ActionResult<EngagementTypeSettings> Get() => store.EngagementTypeSettings;

    [HttpPut]
    public ActionResult<EngagementTypeSettings> Save([FromBody] EngagementTypeSettings request)
    {
        if (request is null) return BadRequest(new { message = "Settings payload is required." });
        store.ApplyEngagementTypes(request);
        var enabled = store.EngagementTypeSettings.Types.Count(t => t.Enabled);
        audit.Log("Settings", "Engagement types saved",
            $"{enabled} of {store.EngagementTypeSettings.Types.Count} engagement types enabled.",
            "Engagement Types", "engagement-types");
        return Ok(store.EngagementTypeSettings);
    }

    [HttpPost("reset")]
    public ActionResult<EngagementTypeSettings> Reset()
    {
        store.ResetEngagementTypes();
        audit.Log("Settings", "Engagement types reset", "Restored the built-in engagement catalog.",
            "Engagement Types", "engagement-types");
        return Ok(store.EngagementTypeSettings);
    }
}
