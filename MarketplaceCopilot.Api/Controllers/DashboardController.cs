using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController(DataStore store, ITenantAccessor tenant) : ControllerBase
{
    /// <summary>Counters for the Home "Engagement Insights" card.</summary>
    [HttpGet("insights")]
    public ActionResult<DashboardInsights> GetInsights([FromServices] ISnapshotService snapshots)
        => snapshots.BuildDashboardInsights();

    [HttpGet]
    public ActionResult<DashboardSummary> Get()
    {
        var deals = store.Deals.Where(d => !d.Archived && d.TenantId == tenant.TenantId).ToList();
        var openDeals = deals
            .Where(d => d.MarketplaceStatus is not "Published" and not "Abandoned")
            .OrderByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.Id)
            .ToList();

        var pipeline = openDeals.Sum(d => d.ExpectedValue);

        var tasks = deals
            .Where(d => d.MeetingNotes?.ActionItems?.Count > 0)
            .SelectMany(d => d.MeetingNotes!.ActionItems.Select(a => new DashboardTaskRow
            {
                Id = a.Id,
                Task = a.Task,
                Deal = d.Id,
                DealName = d.Name,
                Customer = d.Customer,
                DueDate = a.DueDate,
                Status = a.Status
            }))
            .ToList();

        if (tasks.Count == 0)
        {
            tasks = openDeals
                .Where(d => d.StepNumber < 5)
                .Select(d => new DashboardTaskRow
                {
                    Id = d.Id,
                    Task = $"Continue {d.Stage.ToLower()} for {d.Customer}",
                    Deal = d.Id,
                    DealName = d.Name,
                    Customer = d.Customer,
                    DueDate = "Today",
                    Status = d.StepNumber >= 3 ? "In Progress" : "Pending"
                })
                .ToList();
        }

        var reminders = deals
            .Where(d => d.MeetingNotes?.Reminders?.Count > 0)
            .SelectMany(d => d.MeetingNotes!.Reminders.Select(r => new DashboardReminderRow
            {
                Id = r.Id,
                Reminder = r.Reminder,
                Deal = d.Id,
                DealName = d.Name,
                Customer = d.Customer,
                DateTime = r.DateTime,
                Type = r.Type
            }))
            .ToList();

        if (reminders.Count == 0)
        {
            reminders = openDeals
                .Where(d => d.Stage is "Meeting Notes" or "Approval" or "Pricing")
                .Select(d => new DashboardReminderRow
                {
                    Id = d.Id,
                    Reminder = d.Stage switch
                    {
                        "Pricing" => $"Finalize pricing for {d.Customer}",
                        "Meeting Notes" => $"Review notes for {d.Customer}",
                        _ => $"Approval needed for {d.Customer}"
                    },
                    Deal = d.Id,
                    DealName = d.Name,
                    Customer = d.Customer,
                    DateTime = d.LastUpdated,
                    Type = d.Stage == "Pricing" ? "Follow-up" : "Meeting"
                })
                .ToList();
        }

        // Recent activity across all engagements — flatten each deal's change history,
        // newest first, so the Home widget shows what just happened and links to the engagement.
        var recentActivity = deals
            .Where(d => d.ChangeHistory is { Count: > 0 })
            .SelectMany(d => d.ChangeHistory.Select(h => new RecentActivityRow
            {
                Id = h.Id,
                DealId = d.Id,
                DealName = string.IsNullOrWhiteSpace(d.Name) ? d.Id : d.Name,
                Customer = d.Customer,
                Category = h.Category,
                Summary = h.Summary,
                Details = h.Details,
                ChangedBy = h.ChangedBy,
                Timestamp = h.Timestamp
            }))
            .OrderByDescending(a => DateTime.TryParse(a.Timestamp.Replace(" UTC", ""), out var dt) ? dt : DateTime.MinValue)
            .Take(15)
            .ToList();

        var recommendations = new List<string>();
        var approvalCount = deals.Count(d => d.Stage == "Approval");
        if (approvalCount > 0)
            recommendations.Add($"{approvalCount} deal(s) waiting for approval");
        var draftCount = deals.Count(d => d.MarketplaceStatus == "Draft");
        if (draftCount > 0)
            recommendations.Add($"{draftCount} draft deal(s) need product or pricing setup");
        var highDiscount = deals.Count(d => d.Pricing?.DiscountPercent > 20);
        if (highDiscount > 0)
            recommendations.Add($"{highDiscount} deal(s) have discount above 20% — review before submit");
        if (recommendations.Count == 0)
            recommendations.Add("All deals are on track. Create a new deal or continue an open one.");

        return new DashboardSummary
        {
            OpenDeals = openDeals.Count,
            PendingApprovals = approvalCount,
            OffersSubmitted = deals.Count(d => d.MarketplaceStatus == "In Review"),
            PipelineValue = pipeline >= 1_000_000 ? $"${pipeline / 1_000_000m:0.##}M" : $"${pipeline:N0}",
            OpenDealsList = openDeals.Select(d => new DashboardDealRow
            {
                Id = d.Id,
                Name = d.Name,
                Customer = d.Customer,
                Marketplace = d.Marketplace,
                Stage = d.Stage,
                Value = $"${d.ExpectedValue:N0}",
                CreatedAt = d.CreatedAt,
                LastUpdated = d.LastUpdated,
                StepNumber = d.StepNumber,
                ContinueRoute = GetContinueRoute(d)
            }).ToList(),
            Tasks = tasks,
            Reminders = reminders,
            RecentActivity = recentActivity,
            AiRecommendations = recommendations
        };
    }

    private static string GetContinueRoute(Deal d) => d.StepNumber switch
    {
        >= 5 => "approvals",
        >= 4 => "meeting-notes",
        3 => "pricing",
        2 => "pricing",
        _ => "products"
    };
}
