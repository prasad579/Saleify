using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

/// <summary>
/// Read/update the configurable approval rules (Settings → Approval Rules) that decide which
/// reviews an engagement requires (discount/duration thresholds, reviewers, engagement types).
/// </summary>
[ApiController]
[Route("api/approval-rules")]
public class ApprovalRulesController(DataStore store, IAuditService audit) : ControllerBase
{
    [HttpGet]
    public ActionResult<ApprovalRulesSettings> Get() => store.ApprovalRulesSettings;

    [HttpPut]
    public ActionResult<ApprovalRulesSettings> Save([FromBody] ApprovalRulesSettings request)
    {
        if (request is null) return BadRequest(new { message = "Settings payload is required." });
        store.ApplyApprovalRules(request);
        var enabled = store.ApprovalRulesSettings.Rules.Count(r => r.Enabled);
        audit.Log("Settings", "Approval rules saved",
            $"{enabled} of {store.ApprovalRulesSettings.Rules.Count} review rules enabled.",
            "Approval Rules", "approval-rules");
        return Ok(store.ApprovalRulesSettings);
    }

    [HttpPost("reset")]
    public ActionResult<ApprovalRulesSettings> Reset()
    {
        store.ResetApprovalRules();
        audit.Log("Settings", "Approval rules reset", "Restored the built-in approval rules.",
            "Approval Rules", "approval-rules");
        return Ok(store.ApprovalRulesSettings);
    }
}
