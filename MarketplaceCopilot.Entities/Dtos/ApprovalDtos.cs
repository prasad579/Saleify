namespace MarketplaceCopilot.Entities;

public class ApprovalSummary
{
    public string Version { get; set; } = "";
    public string LastUpdatedAt { get; set; } = "";
    public string LastUpdatedBy { get; set; } = "";
    public bool ChangesPendingReapproval { get; set; }
    public string? ChangeSummary { get; set; }
    public bool DocumentsLocked { get; set; }
    public bool AllowPostApprovalEdits { get; set; } = true;
    public string EulaStatus { get; set; } = "Draft";
    public string PackageSummaryId { get; set; } = "";
    public string NextStep { get; set; } = "";
    public List<ApprovalRuleMatch> RuleMatches { get; set; } = [];
    public List<ApprovalStep> Steps { get; set; } = [];
    public List<ApprovalDocument> Documents { get; set; } = [];
    public List<ApprovalAuditEntry> AuditLog { get; set; } = [];
    public ApprovalProgress Progress { get; set; } = new();
    public ApprovalDocument? EulaDocument { get; set; }
    public ApprovalDocument? PackageDocument { get; set; }
}

public class ApprovalProgress
{
    public int Total { get; set; }
    public int Approved { get; set; }
    public int Pending { get; set; }
    public int ChangesRequested { get; set; }
    public int Rejected { get; set; }
    public int Percent { get; set; }
}

public class ApprovalActionRequest
{
    public string StepId { get; set; } = "";
    public string Action { get; set; } = "";
    public string? Comment { get; set; }
    public string? User { get; set; }
}

public class ApprovalActionResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public ApprovalData? Approvals { get; set; }
}
