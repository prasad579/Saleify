using System.Text.RegularExpressions;
using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;

namespace MarketplaceCopilot.Services;

public class AiService : IAiService
{
    private static readonly (string Key, string Label, Func<string, string> Extract)[] StandardTemplate =
    [
        ("contractDuration", "Contract duration", ExtractDuration),
        ("discountRequested", "Discount requested", ExtractDiscount),
        ("paymentModel", "Payment model", ExtractPaymentModel),
        ("legalReview", "Legal review", ExtractLegalReview),
        ("customerInterest", "Customer interest", ExtractInterest)
    ];

    public AiExtractedSummary ExtractInsights(string notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return new AiExtractedSummary
            {
                StandardFields = StandardTemplate.Select(t => new SummaryFieldRow
                {
                    Key = t.Key,
                    Label = t.Label,
                    Value = "Not specified"
                }).ToList()
            };
        }

        return new AiExtractedSummary
        {
            StandardFields = StandardTemplate.Select(t => new SummaryFieldRow
            {
                Key = t.Key,
                Label = t.Label,
                Value = t.Extract(notes)
            }).ToList(),
            DynamicFields = BuildDynamicFields(notes)
        };
    }

    public List<ActionItem> SuggestActionItems(string notes)
    {
        var items = new List<ActionItem>();
        if (string.IsNullOrWhiteSpace(notes)) return items;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string task, string? contextForDate = null)
        {
            var key = Regex.Replace(task, @"\s+", " ").Trim().ToLowerInvariant();
            if (!seen.Add(key)) return;

            var (due, hint) = ResolveDueDate(notes, contextForDate ?? notes);
            items.Add(MakeItem(task, due, hint));
        }

        if (Regex.IsMatch(notes, @"setup\s+(?:another\s+)?meeting|schedule\s+(?:a\s+)?meeting|meet\s+again|next\s+meeting|follow[\s-]?up\s+meeting", RegexOptions.IgnoreCase))
            Add("Schedule follow-up meeting", ExtractMeetingContext(notes));

        if (ExtractLegalReview(notes) == "Required")
            Add("Schedule legal review", "next week");

        if (Regex.IsMatch(notes, @"technical\s+review|architecture\s+review|solution\s+review|tech\s+review", RegexOptions.IgnoreCase))
            Add("Complete technical review", notes);

        if (Regex.IsMatch(notes, @"security\s+review|infosec|soc\s*2|pen\s*test", RegexOptions.IgnoreCase))
            Add("Complete security review", notes);

        if (Regex.IsMatch(notes, @"review\s+point|needs?\s+review|pending\s+review|review\s+(?:needed|required|before)|internal\s+review", RegexOptions.IgnoreCase))
            Add("Address review points from meeting", notes);

        if (Regex.IsMatch(notes, @"deck|presentation|product\s+deck|demo\s+material", RegexOptions.IgnoreCase))
            Add("Share product deck / demo materials", "tomorrow");

        if (Regex.IsMatch(notes, @"follow[\s-]?up|call\s+back|check[\s-]?in|reach\s+out", RegexOptions.IgnoreCase))
            Add("Customer follow-up call", "in 3 days");

        if (Regex.IsMatch(notes, @"approval|sign[\s-]?off|manager\s+review|executive\s+approval|stakeholder\s+approval", RegexOptions.IgnoreCase))
            Add("Submit for internal approval", "this week");

        if (Regex.IsMatch(notes, @"proposal|quote|pricing\s+sheet|revised\s+offer|private\s+offer", RegexOptions.IgnoreCase))
            Add("Send updated proposal / quote", "in 2 days");

        if (Regex.IsMatch(notes, @"negotiat|counter[\s-]?offer|revise\s+terms|term\s+sheet", RegexOptions.IgnoreCase))
            Add("Follow up on contract negotiation", notes);

        if (Regex.IsMatch(notes, @"poc|pilot|proof\s+of\s+concept|trial", RegexOptions.IgnoreCase))
            Add("Plan POC / pilot scope", notes);

        if (Regex.IsMatch(notes, @"go[\s-]?live|implementation|onboarding|kick[\s-]?off|deployment", RegexOptions.IgnoreCase))
            Add("Confirm implementation / go-live plan", notes);

        if (Regex.IsMatch(notes, @"budget|procurement|purchase\s+order|po\s+process", RegexOptions.IgnoreCase))
            Add("Align on budget / procurement steps", notes);

        if (Regex.IsMatch(notes, @"legal\s+team|counsel|msa|dpa|contract\s+redline", RegexOptions.IgnoreCase)
            && ExtractLegalReview(notes) != "Not required")
            Add("Coordinate with legal on contract terms", notes);

        return items;
    }
        
    public string BuildInsight(string notes, AiExtractedSummary summary)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return "Paste or type meeting notes, then click Extract Insights.";

        var discount = ParsePercent(GetStandard(summary, "discountRequested"));
        var legal = GetStandard(summary, "legalReview");
        var interest = GetStandard(summary, "customerInterest");

        if (discount > 20)
            return $"Customer requested {GetStandard(summary, "discountRequested")}. High discount — route to approvals and legal before updating pricing.";
        if (legal == "Required")
            return "Legal review is required based on your notes. Schedule it before proceeding to approvals.";
        if (interest.Contains("Positive", StringComparison.OrdinalIgnoreCase))
            return "Customer interest looks positive. Capture action items and move to approvals.";
        var actionItems = SuggestActionItems(notes);
        if (actionItems.Count == 0)
            return "Notes captured. Add follow-ups, meetings, or review keywords to generate action items.";
        var missingDates = actionItems.Count(a => string.IsNullOrWhiteSpace(a.DueDate));
        if (missingDates > 0)
            return $"Extracted {actionItems.Count} action item(s). {missingDates} need a due date — pick dates in the table below.";
        return $"Extracted {actionItems.Count} action item(s) from your latest notes.";
    }

    public string Chat(string message, Deal? deal, DataStore store, string tenantId)
    {
        var lower = message.ToLowerInvariant();
        if (lower.Contains("discount"))
        {
            var pct = deal?.Pricing?.DiscountPercent;
            if (pct is null)
                return "No pricing configured yet. Open Configure Pricing for this deal first.";
            return $"Deal {deal!.Id} ({deal.Customer}) has a {pct}% discount. Net value: ${deal.ExpectedValue:N0}.";
        }
        if (lower.Contains("approval") || lower.Contains("pending"))
        {
            var tenantDeals = store.Deals.Where(d => d.TenantId == tenantId).ToList();
            var pending = tenantDeals.Count(d => d.Stage is "Approval" or "Meeting Notes");
            return pending > 0
                ? $"{pending} deal(s) need approval or follow-up. Check {string.Join(", ", tenantDeals.Where(d => d.Stage is "Approval").Select(d => d.Id))}."
                : "No deals are currently waiting for approval.";
        }
        if (lower.Contains("pricing") || lower.Contains("calculate"))
        {
            if (deal?.Pricing is null)
                return "Select products first — list price drives the pricing calculator.";
            return $"Pricing for {deal.Id}: ${deal.Pricing.PublicPricePerYear:N0}/yr, {deal.Pricing.DiscountPercent}% off, net ${deal.Pricing.NetContractValue:N0}.";
        }
        if (lower.Contains("product"))
        {
            var count = deal?.ProductIds.Count ?? 0;
            return count > 0
                ? $"This deal has {count} product(s) selected. Open Select Products to change them."
                : "No products selected yet. Go to Select Products in the deal wizard.";
        }
        return "Ask about discount, pricing, approvals, or products for the current deal.";
    }

    private static List<SummaryFieldRow> BuildDynamicFields(string notes)
    {
        var fields = new List<SummaryFieldRow>();

        void Add(string key, string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Equals("Not mentioned", StringComparison.OrdinalIgnoreCase))
                return;
            fields.Add(new SummaryFieldRow { Key = key, Label = label, Value = value });
        }

        Add("followUpMeeting", "Follow-up meeting", ExtractFollowUpMeeting(notes));
        Add("technicalReview", "Technical review", ExtractFlag(notes, @"technical\s+review|architecture\s+review|solution\s+review", "Discussed / required", "Not mentioned"));
        Add("securityReview", "Security review", ExtractFlag(notes, @"security\s+review|infosec|soc\s*2", "Discussed / required", "Not mentioned"));
        Add("reviewPoints", "Review points", ExtractReviewPoints(notes));
        Add("stakeholders", "Stakeholders", ExtractStakeholders(notes));
        Add("implementation", "Implementation / go-live", ExtractImplementation(notes));
        Add("competitor", "Competitor mention", ExtractCompetitor(notes));
        Add("budgetNotes", "Budget / procurement", ExtractBudget(notes));
        Add("pocPilot", "POC / pilot", ExtractFlag(notes, @"poc|pilot|proof\s+of\s+concept|trial", "Requested", "Not mentioned"));
        Add("contractTerms", "Contract terms", ExtractContractTerms(notes));

        return fields;
    }

    private static string GetStandard(AiExtractedSummary summary, string key) =>
        summary.StandardFields.FirstOrDefault(f => f.Key == key)?.Value ?? "";

    private static ActionItem MakeItem(string task, string dueIso, string hint) => new()
    {
        Id = Guid.NewGuid().ToString("N")[..8],
        Task = task,
        Owner = "Srinivas K",
        DueDate = dueIso,
        DueDateHint = hint,
        Status = "Pending",
        Source = "ai"
    };

    private static (string DueIso, string Hint) ResolveDueDate(string notes, string context)
    {
        var parsed = ParseDueDate(context);
        if (parsed.HasValue)
            return (parsed.Value.ToString("yyyy-MM-dd"), BuildDateHint(context, parsed.Value));

        var relative = ExtractRelativeDatePhrase(context);
        if (!string.IsNullOrWhiteSpace(relative))
            return ("", $"{relative} (from notes — pick a date)");

        return ("", "No date in notes — add due date");
    }

    private static string BuildDateHint(string context, DateTime date)
    {
        var phrase = ExtractRelativeDatePhrase(context);
        if (!string.IsNullOrWhiteSpace(phrase))
            return $"{phrase} → {date:MMM d, yyyy}";
        return date.ToString("MMM d, yyyy");
    }

    private static string ExtractRelativeDatePhrase(string text)
    {
        var patterns = new[]
        {
            @"(?:next\s+week\s*\(\s*\w+\s*\)|next\s+week\s+\(\s*\w+\s*\))",
            @"next\s+\w+day",
            @"this\s+\w+day",
            @"next\s+week",
            @"this\s+week",
            @"tomorrow",
            @"in\s+\d+\s+days?",
            @"in\s+\d+\s+weeks?",
            @"by\s+\w+day",
            @"end\s+of\s+(?:the\s+)?week"
        };
        foreach (var p in patterns)
        {
            var m = Regex.Match(text, p, RegexOptions.IgnoreCase);
            if (m.Success) return m.Value.Trim();
        }
        return "";
    }

    private static string ExtractMeetingContext(string notes)
    {
        var m = Regex.Match(notes, @"(?:meeting|call)[^.!\n]*(?:next\s+week[^.!\n]*|friday|monday|tuesday|wednesday|thursday|saturday|sunday)[^.!\n]*", RegexOptions.IgnoreCase);
        if (m.Success) return m.Value;
        m = Regex.Match(notes, @"(?:next\s+week[^.!\n]*\([^)]+\)|next\s+week|tomorrow|in\s+\d+\s+days?)", RegexOptions.IgnoreCase);
        return m.Success ? m.Value : notes;
    }

    private static DateTime? ParseDueDate(string text)
    {
        var today = DateTime.UtcNow.Date;
        var lower = text.ToLowerInvariant();

        foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
        {
            var name = day.ToString().ToLowerInvariant();
            if (!Regex.IsMatch(lower, $@"(?:next\s+week\s*\(\s*)?{name}|next\s+{name}|this\s+{name}|by\s+{name}"))
                continue;

            var daysUntil = ((int)day - (int)today.DayOfWeek + 7) % 7;
            if (daysUntil == 0) daysUntil = 7;
            if (Regex.IsMatch(lower, @"next\s+week"))
                daysUntil += 7;
            else if (Regex.IsMatch(lower, $@"this\s+{name}"))
                daysUntil = daysUntil == 7 ? 0 : daysUntil;

            return today.AddDays(daysUntil);
        }

        if (Regex.IsMatch(lower, @"tomorrow"))
            return today.AddDays(1);

        var inDays = Regex.Match(lower, @"in\s+(\d+)\s+days?");
        if (inDays.Success && int.TryParse(inDays.Groups[1].Value, out var d))
            return today.AddDays(d);

        var inWeeks = Regex.Match(lower, @"in\s+(\d+)\s+weeks?");
        if (inWeeks.Success && int.TryParse(inWeeks.Groups[1].Value, out var w))
            return today.AddDays(w * 7);

        if (Regex.IsMatch(lower, @"next\s+week"))
            return today.AddDays(7);

        if (Regex.IsMatch(lower, @"this\s+week|end\s+of\s+(?:the\s+)?week"))
        {
            var daysUntilFriday = ((int)DayOfWeek.Friday - (int)today.DayOfWeek + 7) % 7;
            return today.AddDays(daysUntilFriday == 0 ? 7 : daysUntilFriday);
        }

        return null;
    }

    private static string ExtractDuration(string notes)
    {
        var year = Regex.Match(notes, @"(\d+)\s*-?\s*years?", RegexOptions.IgnoreCase);
        if (year.Success) return $"{year.Groups[1].Value} year(s)";
        var month = Regex.Match(notes, @"(\d+)\s*-?\s*months?", RegexOptions.IgnoreCase);
        if (month.Success) return $"{month.Groups[1].Value} month(s)";
        var day = Regex.Match(notes, @"(\d+)\s*-?\s*days?", RegexOptions.IgnoreCase);
        if (day.Success) return $"{day.Groups[1].Value} day(s)";
        return "Not specified in notes";
    }

    private static string ExtractDiscount(string notes)
    {
        var pct = Regex.Match(notes, @"(\d+(?:\.\d+)?)\s*%", RegexOptions.IgnoreCase);
        return pct.Success ? $"{pct.Groups[1].Value}%" : "Not specified in notes";
    }

    private static string ExtractPaymentModel(string notes)
    {
        if (Regex.IsMatch(notes, @"flexible\s+payment|fps|installment", RegexOptions.IgnoreCase)) return "Flexible payment schedule (FPS)";
        if (Regex.IsMatch(notes, @"quarterly", RegexOptions.IgnoreCase)) return "Quarterly";
        if (Regex.IsMatch(notes, @"monthly", RegexOptions.IgnoreCase)) return "Monthly";
        if (Regex.IsMatch(notes, @"annual|yearly|per year", RegexOptions.IgnoreCase)) return "Annual";
        return "Not specified in notes";
    }

    private static string ExtractLegalReview(string notes)
    {
        if (Regex.IsMatch(notes, @"legal\s+review\s+not\s+required|legal\s+not\s+required|no\s+legal\s+review|without\s+legal|legal\s+review\s+not\s+needed", RegexOptions.IgnoreCase))
            return "Not required";
        if (Regex.IsMatch(notes, @"legal|compliance|contract\s+review|counsel|msa|dpa", RegexOptions.IgnoreCase))
            return "Required";
        return "Not mentioned";
    }

    private static string ExtractInterest(string notes)
    {
        if (Regex.IsMatch(notes, @"positive|interested|keen|excited|agreed|showed\s+interest|approved", RegexOptions.IgnoreCase))
            return "Interested / Positive";
        if (Regex.IsMatch(notes, @"concern|hesitant|delay|push[\s-]?back|not\s+sure|cold", RegexOptions.IgnoreCase))
            return "Cautious / Needs follow-up";
        return "Neutral";
    }

    private static string ExtractFollowUpMeeting(string notes)
    {
        if (!Regex.IsMatch(notes, @"meeting|call|sync|workshop", RegexOptions.IgnoreCase))
            return "Not mentioned";

        var m = Regex.Match(notes, @"(?:setup|schedule|set\s+up|plan|arrange)[^.!\n]*(?:meeting|call)[^.!\n]*", RegexOptions.IgnoreCase);
        if (m.Success) return $"Requested — {TrimSentence(m.Value)}";

        m = Regex.Match(notes, @"(?:next\s+meeting|follow[\s-]?up\s+(?:meeting|call))[^.!\n]*", RegexOptions.IgnoreCase);
        if (m.Success) return TrimSentence(m.Value);

        return "Mentioned in notes";
    }

    private static string ExtractReviewPoints(string notes)
    {
        var parts = new List<string>();
        foreach (Match m in Regex.Matches(notes, @"(?:review\s+point[s]?|needs?\s+review|pending\s+review|(?:\w+\s+){0,2}review\s+(?:needed|required))[^.!\n]*", RegexOptions.IgnoreCase))
            parts.Add(TrimSentence(m.Value));
        return parts.Count > 0 ? string.Join("; ", parts.Distinct()) : "Not mentioned";
    }

    private static string ExtractStakeholders(string notes)
    {
        var m = Regex.Match(notes, @"(?:stakeholder[s]?|decision[\s-]?maker|executive[s]?|legal\s+team|procurement|CFO|CTO|CEO)[^.!\n]*", RegexOptions.IgnoreCase);
        return m.Success ? TrimSentence(m.Value) : "Not mentioned";
    }

    private static string ExtractImplementation(string notes)
    {
        var m = Regex.Match(notes, @"(?:go[\s-]?live|implementation|onboarding|kick[\s-]?off|deployment|rollout)[^.!\n]*", RegexOptions.IgnoreCase);
        return m.Success ? TrimSentence(m.Value) : "Not mentioned";
    }

    private static string ExtractCompetitor(string notes)
    {
        var m = Regex.Match(notes, @"(?:competitor|alternative|versus|vs\.?|compared\s+to)[^.!\n]*", RegexOptions.IgnoreCase);
        return m.Success ? TrimSentence(m.Value) : "Not mentioned";
    }

    private static string ExtractBudget(string notes)
    {
        var m = Regex.Match(notes, @"(?:budget|procurement|purchase\s+order|po\s+process|cost\s+approval)[^.!\n]*", RegexOptions.IgnoreCase);
        return m.Success ? TrimSentence(m.Value) : "Not mentioned";
    }

    private static string ExtractContractTerms(string notes)
    {
        var m = Regex.Match(notes, @"(?:msa|dpa|term\s+sheet|renewal|auto[\s-]?renew|termination|sla)[^.!\n]*", RegexOptions.IgnoreCase);
        return m.Success ? TrimSentence(m.Value) : "Not mentioned";
    }

    private static string ExtractFlag(string notes, string pattern, string whenMatch, string whenNot) =>
        Regex.IsMatch(notes, pattern, RegexOptions.IgnoreCase) ? whenMatch : whenNot;

    private static string TrimSentence(string value)
    {
        var t = value.Trim().TrimEnd('.', '!', '?');
        return t.Length > 120 ? t[..117] + "…" : t;
    }

    private static decimal ParsePercent(string value)
    {
        var m = Regex.Match(value ?? "", @"(\d+(?:\.\d+)?)");
        return m.Success ? decimal.Parse(m.Groups[1].Value) : 0;
    }
}
