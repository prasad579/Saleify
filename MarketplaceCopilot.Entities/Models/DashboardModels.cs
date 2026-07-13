namespace MarketplaceCopilot.Entities;

public class DashboardSummary
{
    public int OpenDeals { get; set; }
    public int PendingApprovals { get; set; }
    public int OffersSubmitted { get; set; }
    public string PipelineValue { get; set; } = "";
    public List<DashboardDealRow> OpenDealsList { get; set; } = [];
    public List<DashboardTaskRow> Tasks { get; set; } = [];
    public List<DashboardReminderRow> Reminders { get; set; } = [];
    public List<RecentActivityRow> RecentActivity { get; set; } = [];
    public List<string> AiRecommendations { get; set; } = [];
}

/// <summary>
/// A single recent change/activity across engagements (aggregated from each deal's change history),
/// with the related engagement so the Home widget can link straight to it.
/// </summary>
public class RecentActivityRow
{
    public string Id { get; set; } = "";
    public string DealId { get; set; } = "";
    public string DealName { get; set; } = "";
    public string Customer { get; set; } = "";
    public string Category { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Details { get; set; } = "";
    public string ChangedBy { get; set; } = "";
    public string Timestamp { get; set; } = "";
}

public class DashboardDealRow
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Customer { get; set; } = "";
    public string Marketplace { get; set; } = "";
    public string Stage { get; set; } = "";
    public string Value { get; set; } = "";
    public string CreatedAt { get; set; } = "";
    public string LastUpdated { get; set; } = "";
    public int StepNumber { get; set; }
    public string ContinueRoute { get; set; } = "pricing";
}

public class DashboardTaskRow
{
    public string Id { get; set; } = "";
    public string Task { get; set; } = "";
    public string Deal { get; set; } = "";
    public string DealName { get; set; } = "";
    public string Customer { get; set; } = "";
    public string DueDate { get; set; } = "";
    public string Status { get; set; } = "";
}

public class DashboardReminderRow
{
    public string Id { get; set; } = "";
    public string Reminder { get; set; } = "";
    public string Deal { get; set; } = "";
    public string DealName { get; set; } = "";
    public string Customer { get; set; } = "";
    public string DateTime { get; set; } = "";
    public string Type { get; set; } = "";
}

public class DealStats
{
    public int Total { get; set; }
    public int Draft { get; set; }
    public int InProgress { get; set; }
    public int Submitted { get; set; }
    public int Accepted { get; set; }
    public int Abandoned { get; set; }
}

public class AuthRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? FullName { get; set; }
    /// <summary>Company/org name at signup — resolves or creates the tenant this user belongs to.</summary>
    public string? Company { get; set; }
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Token { get; set; }
    public string? Role { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Status { get; set; }
    public string? Provider { get; set; }
    public string? Company { get; set; }
    public string? TenantId { get; set; }
    public string? TenantName { get; set; }
}

public class ExtractInsightsRequest
{
    public string Notes { get; set; } = "";
}

public class CopilotChatRequest
{
    public string Message { get; set; } = "";
    public string? DealId { get; set; }
}

public class CopilotChatResponse
{
    public string Reply { get; set; } = "";
}
