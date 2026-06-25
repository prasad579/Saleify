using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

/// <summary>
/// Read/update the configurable approval rules (Settings → Approval Rules) that decide which
/// reviews an engagement requires (discount/duration thresholds, reviewers, engagement types).
/// </summary>
[ApiController]
[Route("api/approval-rules")]
public class ApprovalRulesController(DataStore store) : ControllerBase
{
    [HttpGet]
    public ActionResult<ApprovalRulesSettings> Get() => store.ApprovalRulesSettings;

    [HttpPut]
    public ActionResult<ApprovalRulesSettings> Save([FromBody] ApprovalRulesSettings request)
    {
        if (request is null) return BadRequest(new { message = "Settings payload is required." });
        store.ApplyApprovalRules(request);
        return Ok(store.ApprovalRulesSettings);
    }

    [HttpPost("reset")]
    public ActionResult<ApprovalRulesSettings> Reset()
    {
        store.ResetApprovalRules();
        return Ok(store.ApprovalRulesSettings);
    }
}
