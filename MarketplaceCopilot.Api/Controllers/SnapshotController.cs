using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

[ApiController]
[Route("api/snapshot")]
public class SnapshotController(ISnapshotService snapshots, DataStore store, IAuditService audit) : ControllerBase
{
    /// <summary>Generate an Engagement Snapshot for the requested scope.</summary>
    [HttpPost]
    public ActionResult<EngagementSnapshot> Generate([FromBody] SnapshotRequest request)
        => snapshots.Build(request);

    /// <summary>Build the snapshot and email it (real send when SMTP is configured; preview otherwise).</summary>
    [HttpPost("email")]
    public ActionResult<EmailSummaryResponse> Email([FromBody] EmailSummaryRequest request)
        => snapshots.SendEmail(request);

    /// <summary>Current Snapshot &amp; Email settings (button toggles, sections, fields, labels).</summary>
    [HttpGet("settings")]
    public ActionResult<SnapshotSettings> GetSettings() => store.SnapshotSettings;

    /// <summary>Save Snapshot &amp; Email settings.</summary>
    [HttpPut("settings")]
    public ActionResult<SnapshotSettings> SaveSettings([FromBody] SnapshotSettings request)
    {
        if (request is null) return BadRequest(new { message = "Settings payload is required." });
        store.ApplySnapshotSettings(request);
        audit.Log("Settings", "Snapshot & email settings saved",
            "Updated snapshot/email buttons, sections, and fields.", "Snapshot & Email", "snapshot");
        return Ok(store.SnapshotSettings);
    }

    /// <summary>Reset Snapshot &amp; Email settings to the built-in defaults.</summary>
    [HttpPost("settings/reset")]
    public ActionResult<SnapshotSettings> ResetSettings()
    {
        store.ResetSnapshotSettings();
        audit.Log("Settings", "Snapshot & email settings reset", "Restored the built-in snapshot/email settings.",
            "Snapshot & Email", "snapshot");
        return Ok(store.SnapshotSettings);
    }
}
