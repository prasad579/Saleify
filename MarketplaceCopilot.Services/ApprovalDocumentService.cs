using System.Net;
using System.Text;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;

namespace MarketplaceCopilot.Services;

public class ApprovalDocumentService : IApprovalDocumentService
{
    private const string BrandName = "SaaSify";
    private const string BrandTagline = "Marketplace Copilot · AI-Powered Deal Desk";

    public void GenerateAll(Deal deal, string version, string timestamp, bool allApproved)
    {
        deal.Approvals ??= new ApprovalData();
        var locked = deal.Approvals.DocumentsLocked && allApproved;
        var p = deal.Pricing;
        var fill = allApproved ? 100 : ComputeEulaFillPercent(deal);

        var docs = new List<ApprovalDocument>
        {
            BuildDoc("doc-package", "Approval Package Summary", "package", version, timestamp, locked,
                $"{deal.Id}-approval-package-{version}.html",
                BuildPackageSummary(deal, version, timestamp, allApproved)),
            BuildDoc("doc-1", "Pricing Summary", "pricing", version, timestamp, locked,
                $"{deal.Id}-pricing-summary-{version}.html",
                BuildPricingSummary(deal, version, timestamp)),
            BuildDoc("doc-2", "Financial Analysis", "financial", version, timestamp, locked,
                $"{deal.Id}-financial-analysis-{version}.html",
                BuildFinancialAnalysis(deal, version, timestamp)),
            BuildDoc("doc-3", "Legal Summary", "legal", version, timestamp, locked,
                $"{deal.Id}-legal-summary-{version}.html",
                BuildLegalSummary(deal, version, timestamp)),
            BuildDoc("doc-4", "Marketplace Guidelines", "marketplace", version, timestamp, locked,
                $"{deal.Id}-marketplace-guidelines-{version}.html",
                BuildMarketplaceGuidelines(deal, version, timestamp)),
            BuildDoc("doc-eula", "End User License Agreement (EULA)", "eula", version, timestamp,
                locked && allApproved, $"{deal.Id}-eula-{version}.html",
                BuildEula(deal, version, timestamp, fill), fill)
        };

        deal.Approvals.Documents = docs;
        deal.Approvals.PackageSummaryId = "doc-package";
        deal.Approvals.EulaStatus = allApproved ? "Final" : fill >= 60 ? "Partial" : "Draft";
    }

    public ApprovalDocument? GetDocument(Deal deal, string docId) =>
        deal.Approvals?.Documents.FirstOrDefault(d => d.Id == docId);

    public string GetHtml(Deal deal, string docId)
    {
        var doc = GetDocument(deal, docId);
        if (doc is null) return WrapPage("Document not found", "<p>Document not found.</p>", deal, "—");
        if (string.IsNullOrWhiteSpace(doc.HtmlContent))
            doc.HtmlContent = BuildPlaceholder(deal, doc);
        return doc.HtmlContent;
    }

    public static int ComputeEulaFillPercent(Deal deal)
    {
        var steps = deal.Approvals?.Steps.Where(s => s.Id != "eula").ToList() ?? [];
        if (steps.Count == 0) return 25;
        var approved = steps.Count(s => s.Status == "Approved");
        var basePercent = (int)Math.Round(approved * 70.0 / steps.Count);
        return Math.Clamp(25 + basePercent, 25, 95);
    }

    private static ApprovalDocument BuildDoc(string id, string name, string type, string version, string ts,
        bool locked, string fileName, string html, int fill = 100) => new()
    {
        Id = id,
        Name = name,
        DocumentType = type,
        Version = version,
        GeneratedAt = ts,
        FileName = fileName,
        Locked = locked,
        Stale = false,
        FillPercent = fill,
        HtmlContent = html
    };

    private string BuildPackageSummary(Deal deal, string version, string ts, bool final)
    {
        var p = deal.Pricing;
        var body = new StringBuilder();
        body.Append("<h2>Approval package overview</h2>");
        body.Append($"<p>Deal <strong>{WebUtility.HtmlEncode(deal.Id)}</strong> — {WebUtility.HtmlEncode(deal.Customer)}</p>");
        body.Append("<table class=\"data\">");
        body.AppendRow("Deal name", deal.Name);
        body.AppendRow("Marketplace", deal.Marketplace);
        body.AppendRow("Package version", version);
        body.AppendRow("Generated", ts);
        body.AppendRow("Status", final ? "Locked — all approvals complete" : "Draft — pending approvals");
        if (p != null)
        {
            body.AppendRow("Net contract value", $"${p.NetContractValue:N2}");
            body.AppendRow("Discount", $"{p.DiscountPercent}%");
            body.AppendRow("Duration", $"{p.DurationValue} {p.DurationType}");
        }
        body.Append("</table>");
        body.Append("<h3>Included documents</h3><ul>");
        body.Append("<li>Pricing Summary</li><li>Financial Analysis</li><li>Legal Summary</li>");
        body.Append("<li>Marketplace Guidelines</li><li>End User License Agreement (EULA)</li></ul>");
        return WrapPage("Approval Package Summary", body.ToString(), deal, version);
    }

    private string BuildPricingSummary(Deal deal, string version, string ts)
    {
        var p = deal.Pricing;
        var body = new StringBuilder("<h2>Pricing summary</h2><table class=\"data\">");
        if (p != null)
        {
            body.AppendRow("Offer type", p.OfferType);
            body.AppendRow("Contract start", p.ContractStart);
            body.AppendRow("Contract end", p.ContractEnd);
            body.AppendRow("Public price / year", $"${p.PublicPricePerYear:N2}");
            body.AppendRow("Discount", $"{p.DiscountPercent}%");
            body.AppendRow("Public contract value", $"${p.PublicContractValue:N2}");
            body.AppendRow("Net contract value", $"${p.NetContractValue:N2}");
            body.AppendRow("Total payable", $"${p.TotalPayable:N2}");
            if (p.FlexiblePaymentsEnabled)
                body.AppendRow("Installments", p.InstallmentCount.ToString());
        }
        else body.Append("<tr><td colspan=\"2\">Pricing not configured.</td></tr>");
        body.Append("</table>");
        return WrapPage("Pricing Summary", body.ToString(), deal, version);
    }

    private string BuildFinancialAnalysis(Deal deal, string version, string ts)
    {
        var p = deal.Pricing;
        var body = new StringBuilder("<h2>Financial analysis</h2><table class=\"data\">");
        if (p != null)
        {
            body.AppendRow("Expected deal value", $"${deal.ExpectedValue:N2}");
            body.AppendRow("Total discount", $"${p.TotalDiscount:N2}");
            body.AppendRow("Marketplace fee", $"${p.MarketplaceFee:N2} ({p.MarketplaceFeePercent}%)");
            body.AppendRow("Net before fees", $"${p.NetPriceBeforeFees:N2}");
            if (p.YearlyBreakdown?.Count > 0)
            {
                body.Append("</table><h3>Yearly breakdown</h3><table class=\"data\">");
                body.Append("<tr><th>Year</th><th>Amount</th></tr>");
                foreach (var row in p.YearlyBreakdown)
                    body.Append($"<tr><td>{WebUtility.HtmlEncode(row.Period)}</td><td>${row.YourPrice:N2}</td></tr>");
            }
            if (p.InstallmentSchedule?.Count > 0)
            {
                body.Append("</table><h3>Installment schedule</h3><table class=\"data\">");
                body.Append("<tr><th>#</th><th>Due</th><th>Amount</th></tr>");
                foreach (var row in p.InstallmentSchedule)
                    body.Append($"<tr><td>{row.Number}</td><td>{WebUtility.HtmlEncode(row.DueDate)}</td><td>${row.Amount:N2}</td></tr>");
            }
        }
        else body.Append("<tr><td colspan=\"2\">No pricing data.</td></tr>");
        body.Append("</table>");
        return WrapPage("Financial Analysis", body.ToString(), deal, version);
    }

    private string BuildLegalSummary(Deal deal, string version, string ts)
    {
        var p = deal.Pricing;
        var months = p?.DurationMonths ?? 0;
        var body = new StringBuilder("<h2>Legal summary</h2><table class=\"data\">");
        body.AppendRow("Customer", deal.Customer);
        body.AppendRow("Contact", $"{deal.ContactName} ({deal.ContactEmail})");
        body.AppendRow("Contract duration", months > 0 ? $"{months} months" : "—");
        body.AppendRow("Deal type", deal.DealType);
        body.AppendRow("Legal review required", months > 24 ? "Yes — term exceeds 24 months" : "Standard term");
        body.Append("</table>");
        body.Append("<p class=\"muted\">This summary is generated from deal configuration and meeting notes. Final legal terms subject to counsel review.</p>");
        return WrapPage("Legal Summary", body.ToString(), deal, version);
    }

    private string BuildMarketplaceGuidelines(Deal deal, string version, string ts)
    {
        var body = new StringBuilder("<h2>Marketplace guidelines</h2><table class=\"data\">");
        body.AppendRow("Marketplace", deal.Marketplace);
        body.AppendRow("Marketplace status", deal.MarketplaceStatus);
        body.AppendRow("Offer type", deal.Pricing?.OfferType ?? "Direct Private Offer");
        body.Append("</table>");
        body.Append($"<p>Private offer submission must comply with {WebUtility.HtmlEncode(deal.Marketplace)} marketplace listing and pricing policies.</p>");
        return WrapPage("Marketplace Guidelines", body.ToString(), deal, version);
    }

    private string BuildEula(Deal deal, string version, string ts, int fillPercent)
    {
        var partial = fillPercent < 100;
        var body = new StringBuilder();
        body.Append($"<div class=\"fill-badge\">{(partial ? $"Draft — {fillPercent}% complete" : "Final")}</div>");
        body.Append("<h2>End User License Agreement</h2>");
        body.Append($"<p><strong>Between:</strong> {BrandName}, Inc. (“Licensor”)<br/>");
        body.Append($"<strong>And:</strong> {WebUtility.HtmlEncode(deal.Customer)} (“Licensee”)</p>");

        if (partial)
        {
            body.Append("<div class=\"partial-notice\">Partial draft — remaining sections unlock after all required approvals are complete.</div>");
        }

        body.Append("<h3>1. Grant of license</h3>");
        body.Append($"<p>Subject to payment of fees, Licensor grants Licensee a non-exclusive subscription to {BrandName} platform services for the contract term.</p>");

        if (fillPercent >= 40)
        {
            body.Append("<h3>2. Term &amp; fees</h3>");
            body.Append("<table class=\"data\">");
            if (deal.Pricing != null)
            {
                body.AppendRow("Contract value", $"${deal.Pricing.NetContractValue:N2}");
                body.AppendRow("Term", $"{deal.Pricing.DurationValue} {deal.Pricing.DurationType}");
            }
            body.AppendRow("Effective date", deal.Pricing?.ContractStart ?? deal.ExpectedCloseDate);
            body.Append("</table>");
        }

        if (fillPercent >= 70)
        {
            body.Append("<h3>3. Support &amp; SLA</h3><p>Standard support included. Premium SLA available per order form.</p>");
            body.Append("<h3>4. Confidentiality</h3><p>Each party shall protect the other's confidential information.</p>");
        }

        if (fillPercent >= 100)
        {
            body.Append("<h3>5. Limitation of liability</h3><p>Liability capped to fees paid in the twelve months preceding the claim.</p>");
            body.Append("<h3>6. Governing law</h3><p>State of Delaware, USA.</p>");
            body.Append("<div class=\"signature-block\"><p><strong>Authorized signatory (Licensee):</strong> _________________________</p>");
            body.Append("<p><strong>Date:</strong> _________________________</p></div>");
        }
        else
        {
            body.Append("<p class=\"placeholder-block\">[ Sections 3–6 pending final approval ]</p>");
        }

        return WrapPage("EULA", body.ToString(), deal, version);
    }

    private string BuildPlaceholder(Deal deal, ApprovalDocument doc) =>
        WrapPage(doc.Name, $"<p>Generated document for {WebUtility.HtmlEncode(deal.Customer)}.</p>", deal, doc.Version);

    private const string DocStyles = """
        body { font-family: Inter, Segoe UI, sans-serif; margin: 0; background: #f8fafc; color: #0f172a; }
        .page { max-width: 820px; margin: 24px auto; background: #fff; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden; }
        .brand { background: linear-gradient(135deg, #1e1b4b, #4f46e5); color: #fff; padding: 24px 32px; }
        .brand-logo { font-size: 28px; font-weight: 800; letter-spacing: -0.5px; }
        .brand-logo span { opacity: 0.9; }
        .brand-tag { font-size: 13px; opacity: 0.85; margin-top: 4px; }
        .content { padding: 28px 32px 36px; }
        h2 { margin: 0 0 16px; font-size: 22px; }
        h3 { margin: 24px 0 8px; font-size: 16px; color: #3730a3; }
        table.data { width: 100%; border-collapse: collapse; margin: 12px 0; font-size: 14px; }
        table.data th, table.data td { border: 1px solid #e2e8f0; padding: 10px 12px; text-align: left; }
        table.data th { background: #f1f5f9; font-weight: 600; }
        .footer { padding: 16px 32px; background: #f8fafc; border-top: 1px solid #e2e8f0; font-size: 12px; color: #64748b; }
        .muted { color: #64748b; font-size: 13px; }
        .fill-badge { display: inline-block; background: #ede9fe; color: #5b21b6; padding: 4px 12px; border-radius: 999px; font-size: 12px; font-weight: 700; margin-bottom: 12px; }
        .partial-notice { background: #fffbeb; border: 1px solid #fcd34d; padding: 12px; border-radius: 8px; margin-bottom: 16px; font-size: 13px; }
        .placeholder-block { background: #f1f5f9; padding: 16px; border-radius: 8px; color: #64748b; font-style: italic; }
        .signature-block { margin-top: 32px; padding-top: 16px; border-top: 1px dashed #cbd5e1; }
        """;

    private string WrapPage(string title, string bodyHtml, Deal deal, string version)
    {
        var encTitle = WebUtility.HtmlEncode(title);
        var encCustomer = WebUtility.HtmlEncode(deal.Customer);
        var encVersion = WebUtility.HtmlEncode(version);
        return $"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8"/>
  <title>{encTitle} — {deal.Id}</title>
  <style>
{DocStyles}
  </style>
</head>
<body>
  <div class="page">
    <div class="brand">
      <div class="brand-logo">🤖 <span>{BrandName}</span></div>
      <div class="brand-tag">{BrandTagline}</div>
    </div>
    <div class="content">{bodyHtml}</div>
    <div class="footer">
      Deal {deal.Id} · {encCustomer} · Version {encVersion} · Generated by Marketplace Copilot
    </div>
  </div>
</body>
</html>
""";
    }
}

internal static class HtmlTableBuilder
{
    public static void AppendRow(this StringBuilder sb, string label, string? value) =>
        sb.Append($"<tr><th>{WebUtility.HtmlEncode(label)}</th><td>{WebUtility.HtmlEncode(value ?? "—")}</td></tr>");
}
