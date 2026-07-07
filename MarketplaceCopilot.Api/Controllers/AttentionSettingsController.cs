using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

/// <summary>
/// Read/update the home alert behaviour (Settings → Alerts &amp; Reminders): the "needs attention"
/// alert, the "upcoming this week" card, its look-ahead window, and which sources count.
/// </summary>
[ApiController]
[Route("api/attention-settings")]
public class AttentionSettingsController(DataStore store, IAuditService audit) : ControllerBase
{
    [HttpGet]
    public ActionResult<AttentionSettings> Get() => store.AttentionSettings;

    [HttpPut]
    public ActionResult<AttentionSettings> Save([FromBody] AttentionSettings request)
    {
        if (request is null) return BadRequest(new { message = "Settings payload is required." });
        store.ApplyAttentionSettings(request);
        var s = store.AttentionSettings;
        audit.Log("Settings", "Alerts & reminders saved",
            $"Alert {(s.AlertEnabled ? "on" : "off")}, upcoming {(s.UpcomingEnabled ? "on" : "off")} ({s.UpcomingWindowDays}d).",
            "Alerts & Reminders", "attention-settings");
        return Ok(store.AttentionSettings);
    }

    [HttpPost("reset")]
    public ActionResult<AttentionSettings> Reset()
    {
        store.ResetAttentionSettings();
        audit.Log("Settings", "Alerts & reminders reset", "Restored the default alert settings.",
            "Alerts & Reminders", "attention-settings");
        return Ok(store.AttentionSettings);
    }
}
