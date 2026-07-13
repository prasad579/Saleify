using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarketplaceCopilot.Services;

/// <summary>
/// Builds Engagement Snapshots (executive summaries) from the deal/event stores and
/// renders/sends them as compact HTML email. Pure read over the data store — no mutations.
/// </summary>
public class SnapshotService(DataStore store, IConfiguration config, ILogger<SnapshotService> logger, ITenantAccessor tenant) : ISnapshotService
{
    private const StringComparison Ic = StringComparison.OrdinalIgnoreCase;

    /// <summary>Statuses considered "closed" — excluded from open/attention counts.</summary>
    private static readonly string[] ClosedStatuses = ["Published", "Abandoned", "Completed"];

    private string FrontendUrl => (config["Auth:FrontendUrl"] ?? "http://localhost:4200").TrimEnd('/');

    public EngagementSnapshot Build(SnapshotRequest request)
    {
        var deals = ResolveDeals(request);
        var ev = ResolveEvent(request, deals);
        var title = BuildTitle(request, ev, deals);

        // For a single-engagement snapshot, expose its type so the UI can pick a type-specific
        // layout (e.g. the compact email-style layout used for a Workshop summary).
        var engagementType = string.Equals(request.Scope, "engagement", Ic)
            ? deals.FirstOrDefault()?.EngagementType ?? ""
            : "";

        var snapshot = new EngagementSnapshot
        {
            Title = title,
            Scope = request.Scope,
            EngagementType = engagementType,
            GeneratedAt = DateTime.UtcNow.ToString("dd MMM yyyy, HH:mm 'UTC'", CultureInfo.InvariantCulture),
            SuggestedSubject = $"{title} - Engagement Summary",
            Event = ev is null ? null : new EventInfoSection
            {
                Name = ev.Name,
                StartDate = FormatDate(ev.StartDate),
                EndDate = FormatDate(ev.EndDate),
                Status = ev.Status
            },
            Summary = BuildSummary(deals),
            Pipeline = BuildPipeline(deals),
            Attention = BuildAttention(deals),
            PrivateOffers = BuildPrivateOffers(deals),
            Settings = store.SnapshotSettings
        };
        return snapshot;
    }

    public DashboardInsights BuildDashboardInsights()
    {
        var deals = store.Deals.Where(d => !d.Archived && d.TenantId == tenant.TenantId).ToList();
        return new DashboardInsights
        {
            ActiveEvents = store.CampaignEvents.Count(e => string.Equals(e.Status, "Active", Ic)),
            ActiveEngagements = deals.Count(IsOpen),
            PendingFollowUps = deals.Count(d =>
                (d.MeetingNotes?.ActionItems?.Any(a => string.Equals(a.Status, "Pending", Ic)) ?? false)
                || string.Equals(d.MarketplaceStatus, "Waiting for Info", Ic)),
            PendingApprovals = deals.Count(d => string.Equals(d.Stage, "Approval", Ic))
        };
    }

    public EmailSummaryResponse SendEmail(EmailSummaryRequest request)
    {
        var snapshot = Build(request);
        var subject = string.IsNullOrWhiteSpace(request.Subject) ? snapshot.SuggestedSubject : request.Subject.Trim();
        var body = BuildEmailHtml(snapshot);
        var recipients = request.To.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).ToList();

        var delivered = false;
        string message;

        var smtpHost = config["Email:Smtp:Host"];
        if (!string.IsNullOrWhiteSpace(smtpHost) && recipients.Count > 0)
        {
            try
            {
                SendViaSmtp(smtpHost, recipients, request.Cc, subject, body);
                delivered = true;
                message = $"Summary emailed to {string.Join(", ", recipients)}.";
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SMTP send failed for engagement summary.");
                message = $"SMTP send failed ({ex.Message}). A preview was generated instead.";
            }
        }
        else
        {
            message = recipients.Count > 0
                ? $"Summary prepared for {string.Join(", ", recipients)} — demo mode (no SMTP configured), not actually delivered."
                : "Summary prepared. Add at least one recipient to send.";
        }

        logger.LogInformation("Engagement summary '{Subject}' for {Recipients} (delivered={Delivered}).",
            subject, string.Join(",", recipients), delivered);

        return new EmailSummaryResponse
        {
            Success = true,
            Message = message,
            Subject = subject,
            To = recipients,
            BodyHtml = body,
            Delivered = delivered
        };
    }

    // ---------------- Engagement set resolution ----------------

    private List<Deal> ResolveDeals(SnapshotRequest r)
    {
        // Archived engagements never appear in snapshots/summaries; cross-tenant deals never do either.
        var source = store.Deals.Where(d => !d.Archived && d.TenantId == tenant.TenantId).ToList();
        switch ((r.Scope ?? "").ToLowerInvariant())
        {
            case "engagement":
                return source.Where(d => string.Equals(d.Id, r.DealId, Ic)).ToList();

            case "event":
                return source.Where(d => string.Equals(d.CampaignEventId, r.EventId, Ic)).ToList();

            case "dashboard":
                return source;

            default: // "filtered"
                IEnumerable<Deal> q = source;
                if (r.OpenOnly) q = q.Where(IsOpen);
                if (!string.IsNullOrWhiteSpace(r.Owner)) q = q.Where(d => string.Equals(d.Owner, r.Owner, Ic));
                if (!string.IsNullOrWhiteSpace(r.Stage)) q = q.Where(d => string.Equals(d.Stage, r.Stage, Ic));
                if (!string.IsNullOrWhiteSpace(r.Status)) q = q.Where(d => string.Equals(d.MarketplaceStatus, r.Status, Ic));
                if (!string.IsNullOrWhiteSpace(r.Tag)) q = q.Where(d => string.Equals(d.CampaignEventName, r.Tag, Ic));
                if (!string.IsNullOrWhiteSpace(r.Marketplace)) q = q.Where(d => (d.Marketplace ?? "").Contains(r.Marketplace!, Ic));
                if (!string.IsNullOrWhiteSpace(r.EngagementType)) q = q.Where(d => string.Equals(d.EngagementType, r.EngagementType, Ic));
                if (!string.IsNullOrWhiteSpace(r.Search))
                {
                    var s = r.Search.Trim();
                    q = q.Where(d =>
                        (d.Id ?? "").Contains(s, Ic) ||
                        (d.Name ?? "").Contains(s, Ic) ||
                        (d.Customer ?? "").Contains(s, Ic) ||
                        (d.Marketplace ?? "").Contains(s, Ic) ||
                        (d.Stage ?? "").Contains(s, Ic) ||
                        (d.Owner ?? "").Contains(s, Ic) ||
                        (d.EngagementType ?? "").Contains(s, Ic) ||
                        (d.CampaignEventName ?? "").Contains(s, Ic));
                }
                return q.ToList();
        }
    }

    private CampaignEvent? ResolveEvent(SnapshotRequest r, List<Deal> deals)
    {
        if (string.Equals(r.Scope, "event", Ic) && !string.IsNullOrWhiteSpace(r.EventId))
            return store.CampaignEvents.FirstOrDefault(e => string.Equals(e.Id, r.EventId, Ic));

        if (string.Equals(r.Scope, "engagement", Ic))
        {
            var d = deals.FirstOrDefault();
            if (d is not null && !string.IsNullOrWhiteSpace(d.CampaignEventId))
                return store.CampaignEvents.FirstOrDefault(e => string.Equals(e.Id, d.CampaignEventId, Ic));
        }

        // Filtered by a single tag → show that event.
        if (!string.IsNullOrWhiteSpace(r.Tag))
            return store.CampaignEvents.FirstOrDefault(e => string.Equals(e.Name, r.Tag, Ic));

        return null;
    }

    private static string BuildTitle(SnapshotRequest r, CampaignEvent? ev, List<Deal> deals)
    {
        if (string.Equals(r.Scope, "engagement", Ic))
        {
            var d = deals.FirstOrDefault();
            if (d is not null)
                return !string.IsNullOrWhiteSpace(d.Name) ? d.Name
                    : string.Join(" — ", new[] { d.EngagementType, d.Customer }.Where(s => !string.IsNullOrWhiteSpace(s)));
            return "Engagement";
        }
        if (ev is not null) return ev.Name;
        if (string.Equals(r.Scope, "filtered", Ic))
            return !string.IsNullOrWhiteSpace(r.Tag) ? $"{r.Tag} — Engagements" : "Filtered Engagements";
        return "Engagement Leadership Summary";
    }

    // ---------------- Section builders ----------------

    private static EngagementSummarySection BuildSummary(List<Deal> deals) => new()
    {
        Total = deals.Count,
        ByType = deals
            .GroupBy(d => string.IsNullOrWhiteSpace(d.EngagementType) ? "Other" : d.EngagementType.Trim())
            .Select(g => new SnapshotCount { Label = Pluralize(g.Key), Count = g.Count() })
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.Label, StringComparer.OrdinalIgnoreCase)
            .ToList()
    };

    private PipelineSummarySection BuildPipeline(List<Deal> deals)
    {
        var pipelineDeals = deals.Where(d => !string.Equals(d.MarketplaceStatus, "Abandoned", Ic));
        var value = pipelineDeals.Sum(d => d.ExpectedValue);
        return new PipelineSummarySection
        {
            ExpectedPipelineValue = value,
            ExpectedPipelineDisplay = MoneyFull(value),
            ActivePrivateOffers = deals.Count(d => IsPrivateOffer(d) && IsOpen(d))
        };
    }

    private List<AttentionRow> BuildAttention(List<Deal> deals) =>
        deals.Where(IsOpen)
            .Select(d => new AttentionRow
            {
                Customer = string.IsNullOrWhiteSpace(d.Customer) ? "—" : d.Customer,
                EngagementType = string.IsNullOrWhiteSpace(d.EngagementType) ? "—" : d.EngagementType,
                Owner = string.IsNullOrWhiteSpace(d.Owner) ? "—" : d.Owner,
                Status = CanonicalStatus(d),
                NextActionDate = NextActionDate(d),
                DealId = d.Id,
                Link = Link(d.Id)
            })
            .OrderBy(r => SortKey(r.NextActionDate))
            .Take(50)
            .ToList();

    private List<PrivateOfferRow> BuildPrivateOffers(List<Deal> deals) =>
        deals.Where(d => IsPrivateOffer(d) && IsOpen(d))
            .Select(d =>
            {
                var amount = d.Pricing is { NetContractValue: > 0 } ? d.Pricing.NetContractValue : d.ExpectedValue;
                return new PrivateOfferRow
                {
                    Customer = string.IsNullOrWhiteSpace(d.Customer) ? "—" : d.Customer,
                    Marketplace = string.IsNullOrWhiteSpace(d.Marketplace) ? "—" : d.Marketplace,
                    OfferValue = MoneyShort(amount),
                    Status = CanonicalStatus(d),
                    ExpectedCloseDate = string.IsNullOrWhiteSpace(d.ExpectedCloseDate) ? "—" : FormatDate(d.ExpectedCloseDate),
                    DealId = d.Id,
                    Link = Link(d.Id)
                };
            })
            .Take(50)
            .ToList();

    // ---------------- Derivations ----------------

    private static bool IsOpen(Deal d) => !ClosedStatuses.Contains(d.MarketplaceStatus, StringComparer.OrdinalIgnoreCase);
    private static bool IsPrivateOffer(Deal d) => string.Equals(d.EngagementType, "Private Offer", Ic);

    /// <summary>
    /// The engagement's status, identical to what the Engagements list and the deal overview show
    /// (the marketplace lifecycle status), so the snapshot never disagrees with those views.
    /// </summary>
    private static string CanonicalStatus(Deal d) =>
        !string.IsNullOrWhiteSpace(d.MarketplaceStatus) ? d.MarketplaceStatus
        : !string.IsNullOrWhiteSpace(d.Stage) ? d.Stage
        : "Open";

    /// <summary>Earliest meaningful date: pending action items, then reminders, then expected close date.</summary>
    private static string NextActionDate(Deal d)
    {
        var candidates = new List<string>();
        if (d.MeetingNotes?.ActionItems is { } items)
            candidates.AddRange(items.Where(a => string.Equals(a.Status, "Pending", Ic) && !string.IsNullOrWhiteSpace(a.DueDate)).Select(a => a.DueDate));
        if (d.MeetingNotes?.Reminders is { } reminders)
            candidates.AddRange(reminders.Where(r => !string.IsNullOrWhiteSpace(r.DateTime)).Select(r => r.DateTime));
        if (!string.IsNullOrWhiteSpace(d.ExpectedCloseDate)) candidates.Add(d.ExpectedCloseDate);

        var dated = candidates
            .Select(s => (raw: s, dt: TryParse(s)))
            .Where(x => x.dt.HasValue)
            .OrderBy(x => x.dt!.Value)
            .ToList();
        if (dated.Count > 0) return FormatDate(dated[0].raw);
        return candidates.FirstOrDefault() ?? "—";
    }

    private string Link(string dealId) => $"{FrontendUrl}/deals/{dealId}";

    // ---------------- Formatting helpers ----------------

    private static string Pluralize(string type)
    {
        if (string.IsNullOrWhiteSpace(type)) return "Other";
        var t = type.Trim();
        if (t.EndsWith("s", Ic)) return t;
        // consonant + y → ies (Activity → Activities), but keep vowel + y (Day → Days)
        if (t.Length > 1 && (t[^1] is 'y' or 'Y') && "aeiouAEIOU".IndexOf(t[^2]) < 0)
            return t[..^1] + "ies";
        return t + "s";
    }

    private static string MoneyFull(decimal value) =>
        value >= 1_000_000m
            ? "$" + (value / 1_000_000m).ToString("0.##", CultureInfo.InvariantCulture) + "M"
            : "$" + value.ToString("N0", CultureInfo.InvariantCulture);

    private static string MoneyShort(decimal value)
    {
        if (value >= 1_000_000m) return "$" + (value / 1_000_000m).ToString("0.##", CultureInfo.InvariantCulture) + "M";
        if (value >= 1_000m) return "$" + (value / 1_000m).ToString("0.#", CultureInfo.InvariantCulture) + "K";
        return "$" + value.ToString("N0", CultureInfo.InvariantCulture);
    }

    private static DateTime? TryParse(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt)
            ? dt
            : null;
    }

    /// <summary>Render ISO dates as "dd MMM yyyy"; leave non-ISO strings (e.g. "Today") untouched.</summary>
    private static string FormatDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)
            : value;
    }

    private static long SortKey(string display)
    {
        var dt = TryParse(display);
        return dt?.Ticks ?? long.MaxValue;
    }

    // ---------------- Email rendering / delivery ----------------

    /// <summary>Render the email body, honouring the configured sections, fields, labels, and intro/footer.</summary>
    private static string BuildEmailHtml(EngagementSnapshot s)
    {
        var settings = s.Settings ?? new SnapshotSettings();
        var sb = new StringBuilder();
        sb.Append("<div style=\"font-family:Inter,Segoe UI,Arial,sans-serif;color:#0f172a;max-width:680px;\">");
        sb.Append($"<h2 style=\"margin:0 0 4px;\">{Esc(s.Title)}</h2>");
        sb.Append($"<p style=\"margin:0 0 12px;color:#64748b;font-size:12px;\">Engagement Summary · generated {Esc(s.GeneratedAt)}</p>");
        if (!string.IsNullOrWhiteSpace(settings.EmailIntro))
            sb.Append($"<p style=\"margin:0 0 14px;\">{Esc(settings.EmailIntro)}</p>");

        foreach (var sec in settings.Sections.Where(x => x.Enabled && x.InEmail))
        {
            switch (sec.Key)
            {
                case "eventInfo":
                    if (s.Event is null) break;
                    sb.Append(Heading(sec.Title));
                    sb.Append("<ul style=\"margin:0 0 8px;padding-left:18px;\">");
                    if (FieldOn(sec, "name")) sb.Append($"<li><strong>{Esc(s.Event.Name)}</strong></li>");
                    if (FieldOn(sec, "dates")) sb.Append($"<li>{Esc(s.Event.StartDate)} → {Esc(s.Event.EndDate)}</li>");
                    if (FieldOn(sec, "status")) sb.Append($"<li>{Esc(FieldLabel(sec, "status", "Status"))}: {Esc(s.Event.Status)}</li>");
                    sb.Append("</ul>");
                    break;

                case "engagementSummary":
                    sb.Append(Heading(sec.Title));
                    sb.Append("<ul style=\"margin:0 0 8px;padding-left:18px;\">");
                    if (FieldOn(sec, "total")) sb.Append($"<li><strong>{Esc(FieldLabel(sec, "total", "Total Engagements"))}: {s.Summary.Total}</strong></li>");
                    if (FieldOn(sec, "byType")) foreach (var c in s.Summary.ByType) sb.Append($"<li>{Esc(c.Label)}: {c.Count}</li>");
                    sb.Append("</ul>");
                    break;

                case "pipelineSummary":
                    sb.Append(Heading(sec.Title));
                    sb.Append("<ul style=\"margin:0 0 8px;padding-left:18px;\">");
                    if (FieldOn(sec, "expectedPipeline")) sb.Append($"<li>{Esc(FieldLabel(sec, "expectedPipeline", "Expected Pipeline"))}: <strong>{Esc(s.Pipeline.ExpectedPipelineDisplay)}</strong></li>");
                    if (FieldOn(sec, "activePrivateOffers")) sb.Append($"<li>{Esc(FieldLabel(sec, "activePrivateOffers", "Active Private Offers"))}: {s.Pipeline.ActivePrivateOffers}</li>");
                    sb.Append("</ul>");
                    break;

                case "attention":
                    if (s.Attention.Count == 0) break;
                    sb.Append(Heading(sec.Title));
                    sb.Append(EmailTable(sec, s.Attention.Take(8), AttentionCell));
                    if (s.Attention.Count > 8) sb.Append(More(s.Attention.Count - 8));
                    break;

                case "privateOffers":
                    if (s.PrivateOffers.Count == 0) break;
                    sb.Append(Heading(sec.Title));
                    sb.Append(EmailTable(sec, s.PrivateOffers.Take(8), OfferCell));
                    if (s.PrivateOffers.Count > 8) sb.Append(More(s.PrivateOffers.Count - 8));
                    break;
            }
        }

        if (!string.IsNullOrWhiteSpace(settings.EmailFooter))
            sb.Append($"<p style=\"margin:16px 0 0;color:#64748b;font-size:12px;\">{Esc(settings.EmailFooter)}</p>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string Heading(string title) => $"<h3 style=\"margin:16px 0 6px;\">{Esc(title)}</h3>";
    private static string More(int n) => $"<p style=\"font-size:12px;color:#64748b;\">+ {n} more in the app.</p>";

    private static bool FieldOn(SnapshotSectionSetting sec, string key)
    {
        var f = sec.Fields?.FirstOrDefault(x => x.Key == key);
        return f is null || f.Enabled;
    }

    private static string FieldLabel(SnapshotSectionSetting sec, string key, string fallback)
    {
        var f = sec.Fields?.FirstOrDefault(x => x.Key == key);
        return string.IsNullOrWhiteSpace(f?.Label) ? fallback : f.Label;
    }

    /// <summary>Render a table with only the enabled columns (in configured order) for the given rows.</summary>
    private static string EmailTable<T>(SnapshotSectionSetting sec, IEnumerable<T> rows, Func<T, string, string> cell)
    {
        var fields = sec.Fields.Where(f => f.Enabled).ToList();
        var sb = new StringBuilder();
        sb.Append("<table style=\"border-collapse:collapse;width:100%;font-size:13px;margin-bottom:8px;\"><thead><tr>");
        foreach (var f in fields)
            sb.Append($"<th style=\"text-align:left;border-bottom:2px solid #e2e8f0;padding:6px 8px;\">{Esc(f.Label)}</th>");
        sb.Append("</tr></thead><tbody>");
        foreach (var r in rows)
        {
            sb.Append("<tr>");
            foreach (var f in fields)
                sb.Append($"<td style=\"border-bottom:1px solid #f1f5f9;padding:6px 8px;\">{cell(r, f.Key)}</td>");
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static string AttentionCell(AttentionRow r, string key) => key switch
    {
        "customer" => Esc(r.Customer),
        "engagementType" => Esc(r.EngagementType),
        "owner" => Esc(r.Owner),
        "status" => Esc(r.Status),
        "nextActionDate" => Esc(r.NextActionDate),
        "link" => LinkCell(r.Link),
        _ => ""
    };

    private static string OfferCell(PrivateOfferRow r, string key) => key switch
    {
        "customer" => Esc(r.Customer),
        "marketplace" => Esc(r.Marketplace),
        "offerValue" => Esc(r.OfferValue),
        "status" => Esc(r.Status),
        "expectedCloseDate" => Esc(r.ExpectedCloseDate),
        "link" => LinkCell(r.Link),
        _ => ""
    };

    private static string LinkCell(string url) => $"<a href=\"{Esc(url)}\" style=\"color:#4f46e5;\">View</a>";

    private static string Esc(string? value) => WebUtility.HtmlEncode(value ?? "");

    private void SendViaSmtp(string host, List<string> to, List<string> cc, string subject, string htmlBody)
    {
        var port = int.TryParse(config["Email:Smtp:Port"], out var p) ? p : 587;
        var from = config["Email:From"] ?? "no-reply@marketplacecopilot.local";
        var user = config["Email:Smtp:Username"];
        var pass = config["Email:Smtp:Password"];

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = !string.Equals(config["Email:Smtp:EnableSsl"], "false", Ic),
            Credentials = string.IsNullOrWhiteSpace(user) ? CredentialCache.DefaultNetworkCredentials : new NetworkCredential(user, pass)
        };

        using var message = new MailMessage { From = new MailAddress(from), Subject = subject, Body = htmlBody, IsBodyHtml = true };
        foreach (var addr in to) message.To.Add(addr);
        foreach (var addr in cc.Where(c => !string.IsNullOrWhiteSpace(c))) message.CC.Add(addr.Trim());
        client.Send(message);
    }
}
