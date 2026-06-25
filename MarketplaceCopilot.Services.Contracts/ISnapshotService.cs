using MarketplaceCopilot.Entities;

namespace MarketplaceCopilot.Services.Contracts;

public interface ISnapshotService
{
    /// <summary>Build an Engagement Snapshot for the requested scope.</summary>
    EngagementSnapshot Build(SnapshotRequest request);

    /// <summary>Counters for the Home "Engagement Insights" card.</summary>
    DashboardInsights BuildDashboardInsights();

    /// <summary>
    /// Build the snapshot, render a compact HTML email body, and send it when SMTP is configured.
    /// In demo mode (no SMTP) the body is still generated and returned for preview.
    /// </summary>
    EmailSummaryResponse SendEmail(EmailSummaryRequest request);
}
