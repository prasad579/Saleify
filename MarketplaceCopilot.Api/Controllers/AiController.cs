using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController(DataStore store, IAiService ai) : ControllerBase
{
    [HttpPost("extract-insights")]
    public ActionResult<object> ExtractInsights([FromBody] ExtractInsightsRequest request)
    {
        var summary = ai.ExtractInsights(request.Notes);
        var actionItems = ai.SuggestActionItems(request.Notes);
        return new
        {
            summary,
            actionItems,
            insight = ai.BuildInsight(request.Notes, summary)
        };
    }

    [HttpPost("chat")]
    public ActionResult<CopilotChatResponse> Chat([FromBody] CopilotChatRequest request)
    {
        var deal = string.IsNullOrWhiteSpace(request.DealId)
            ? null
            : store.Deals.FirstOrDefault(d => d.Id == request.DealId);
        return new CopilotChatResponse { Reply = ai.Chat(request.Message, deal, store) };
    }
}
