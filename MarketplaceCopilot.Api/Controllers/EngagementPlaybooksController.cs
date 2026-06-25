using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

[ApiController]
[Route("api/engagement-playbooks")]
public class EngagementPlaybooksController(DataStore store) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<EngagementPlaybook>> GetAll() => store.Playbooks;

    /// <summary>
    /// Upsert a playbook by engagement type (type is in the body to avoid slash-in-route issues,
    /// e.g. "Summit/Event Lead").
    /// </summary>
    [HttpPut]
    public ActionResult<EngagementPlaybook> Upsert([FromBody] EngagementPlaybook request)
    {
        if (string.IsNullOrWhiteSpace(request.EngagementType))
            return BadRequest(new { message = "Engagement type is required." });

        request.NextSteps = Clean(request.NextSteps);
        request.TalkingPoints = Clean(request.TalkingPoints);
        request.Headline = request.Headline?.Trim() ?? "";
        request.Timeline = request.Timeline?.Trim() ?? "";
        request.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC";

        var idx = store.Playbooks.FindIndex(p =>
            string.Equals(p.EngagementType, request.EngagementType, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) store.Playbooks[idx] = request;
        else store.Playbooks.Add(request);

        store.SavePlaybooks();
        return Ok(request);
    }

    [HttpPost("reset")]
    public ActionResult<IEnumerable<EngagementPlaybook>> Reset()
    {
        store.ResetPlaybooks();
        return Ok(store.Playbooks);
    }

    private static List<string> Clean(List<string>? items) =>
        (items ?? []).Select(s => s?.Trim() ?? "").Where(s => s.Length > 0).ToList();
}
