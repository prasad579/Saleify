using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

/// <summary>
/// Read/update the configurable home / dashboard layout (Settings → Home Dashboard) that decides
/// which cards appear on the home page.
/// </summary>
[ApiController]
[Route("api/home-settings")]
public class HomeSettingsController(DataStore store, IAuditService audit) : ControllerBase
{
    [HttpGet]
    public ActionResult<HomeSettings> Get() => store.HomeSettings;

    [HttpPut]
    public ActionResult<HomeSettings> Save([FromBody] HomeSettings request)
    {
        if (request is null) return BadRequest(new { message = "Settings payload is required." });
        store.ApplyHomeSettings(request);
        var enabled = store.HomeSettings.Cards.Count(c => c.Enabled);
        audit.Log("Settings", "Home dashboard saved",
            $"{enabled} of {store.HomeSettings.Cards.Count} home cards enabled.", "Home Dashboard", "home-settings");
        return Ok(store.HomeSettings);
    }

    [HttpPost("reset")]
    public ActionResult<HomeSettings> Reset()
    {
        store.ResetHomeSettings();
        audit.Log("Settings", "Home dashboard reset", "Restored the default home layout.", "Home Dashboard", "home-settings");
        return Ok(store.HomeSettings);
    }
}
