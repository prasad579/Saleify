using System.Text.Json;
using MarketplaceCopilot.Entities;
using Microsoft.Extensions.Hosting;

namespace MarketplaceCopilot.Data;

public class DataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _dealsFilePath;
    private readonly string _eventsFilePath;
    private readonly string _playbooksFilePath;
    private readonly string _snapshotSettingsFilePath;
    private readonly string _approvalRulesFilePath;
    private readonly string _peopleFilePath;
    private readonly string _engagementTypesFilePath;
    private readonly string _homeSettingsFilePath;
    private readonly string _attentionSettingsFilePath;
    private readonly string _offerRequestsFilePath;
    private readonly string _auditLogFilePath;
    private readonly string _engagementRequestsFilePath;

    public List<Deal> Deals { get; private set; } = [];
    public List<CampaignEvent> CampaignEvents { get; private set; } = [];
    public List<EngagementPlaybook> Playbooks { get; private set; } = [];
    public SnapshotSettings SnapshotSettings { get; private set; } = new();
    public ApprovalRulesSettings ApprovalRulesSettings { get; private set; } = new();
    public List<Person> People { get; private set; } = [];
    public EngagementTypeSettings EngagementTypeSettings { get; private set; } = new();
    public HomeSettings HomeSettings { get; private set; } = new();
    public AttentionSettings AttentionSettings { get; private set; } = new();
    public List<OfferRequest> OfferRequests { get; private set; } = [];
    public List<AuditEntry> AuditLog { get; private set; } = [];
    public List<EngagementRequest> EngagementRequests { get; private set; } = [];

    /// <summary>Keep the audit log bounded so the JSON file and memory footprint stay reasonable.</summary>
    private const int MaxAuditEntries = 2000;

    public List<Product> Products { get; } =
    [
        new()
        {
            Id = "prod-1",
            Name = "SaaSify AI Agent – Azure Marketplace Onboarding",
            Description = "AI-powered onboarding assistant for Azure Marketplace listings.",
            Marketplaces = ["Azure", "AWS"],
            ListPricePerYear = 120000
        },
        new()
        {
            Id = "prod-2",
            Name = "SaaSify Contract Intelligence",
            Description = "Automate contract review and compliance checks.",
            Marketplaces = ["Azure", "GCP"],
            ListPricePerYear = 95000
        },
        new()
        {
            Id = "prod-3",
            Name = "CloudLabs Training Suite",
            Description = "Hands-on cloud training labs for enterprise teams.",
            Marketplaces = ["AWS", "Azure", "GCP"],
            ListPricePerYear = 75000
        },
        new()
        {
            Id = "prod-4",
            Name = "C3 Analytics Platform",
            Description = "Enterprise analytics and reporting for marketplace deals.",
            Marketplaces = ["Azure"],
            ListPricePerYear = 110000
        }
    ];

    public DataStore(IHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "data");
        Directory.CreateDirectory(dataDir);
        _dealsFilePath = Path.Combine(dataDir, "deals.json");
        _eventsFilePath = Path.Combine(dataDir, "campaign-events.json");
        _playbooksFilePath = Path.Combine(dataDir, "engagement-playbooks.json");
        _snapshotSettingsFilePath = Path.Combine(dataDir, "snapshot-settings.json");
        _approvalRulesFilePath = Path.Combine(dataDir, "approval-rules.json");
        _peopleFilePath = Path.Combine(dataDir, "people.json");
        _engagementTypesFilePath = Path.Combine(dataDir, "engagement-types.json");
        _homeSettingsFilePath = Path.Combine(dataDir, "home-settings.json");
        _attentionSettingsFilePath = Path.Combine(dataDir, "attention-settings.json");
        _offerRequestsFilePath = Path.Combine(dataDir, "offer-requests.json");
        _auditLogFilePath = Path.Combine(dataDir, "audit-log.json");
        _engagementRequestsFilePath = Path.Combine(dataDir, "engagement-requests.json");
        LoadDeals();
        LoadCampaignEvents();
        LoadPlaybooks();
        LoadSnapshotSettings();
        LoadApprovalRules();
        LoadPeople();
        LoadEngagementTypes();
        LoadHomeSettings();
        LoadAttentionSettings();
        LoadOfferRequests();
        LoadAuditLog();
        LoadEngagementRequests();
    }

    public string NextEngagementRequestId()
    {
        var max = EngagementRequests
            .Select(r => int.TryParse(r.Id.Replace("ER-", "", StringComparison.OrdinalIgnoreCase), out var n) ? n : 4000)
            .DefaultIfEmpty(4000)
            .Max();
        return $"ER-{max + 1}";
    }

    public void SaveEngagementRequests() =>
        File.WriteAllText(_engagementRequestsFilePath, JsonSerializer.Serialize(EngagementRequests, JsonOptions));

    private void LoadEngagementRequests()
    {
        if (!File.Exists(_engagementRequestsFilePath))
        {
            EngagementRequests = [];
            return;
        }
        try
        {
            var loaded = JsonSerializer.Deserialize<List<EngagementRequest>>(File.ReadAllText(_engagementRequestsFilePath), JsonOptions);
            EngagementRequests = loaded ?? [];
        }
        catch
        {
            EngagementRequests = [];
        }
    }

    public string NextOfferRequestId()
    {
        var max = OfferRequests
            .Select(o => int.TryParse(o.Id.Replace("OFR-", "", StringComparison.OrdinalIgnoreCase), out var n) ? n : 1000)
            .DefaultIfEmpty(1000)
            .Max();
        return $"OFR-{max + 1}";
    }

    public void SaveOfferRequests() =>
        File.WriteAllText(_offerRequestsFilePath, JsonSerializer.Serialize(OfferRequests, JsonOptions));

    private void LoadOfferRequests()
    {
        if (!File.Exists(_offerRequestsFilePath))
        {
            OfferRequests = [];
            return;
        }
        try
        {
            var loaded = JsonSerializer.Deserialize<List<OfferRequest>>(File.ReadAllText(_offerRequestsFilePath), JsonOptions);
            OfferRequests = loaded ?? [];
        }
        catch
        {
            OfferRequests = [];
        }
    }

    public string NextPersonId()
    {
        var max = People
            .Select(p => int.TryParse(p.Id.Replace("PER-", "", StringComparison.OrdinalIgnoreCase), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        return $"PER-{max + 1}";
    }

    public string NextDealId()
    {
        var max = Deals
            .Select(d => int.TryParse(d.Id.Replace("DL-", "", StringComparison.OrdinalIgnoreCase), out var n) ? n : 1000)
            .DefaultIfEmpty(1000)
            .Max();
        return $"DL-{max + 1}";
    }

    public string NextCampaignEventId()
    {
        var max = CampaignEvents
            .Select(e => int.TryParse(e.Id.Replace("EVT-", "", StringComparison.OrdinalIgnoreCase), out var n) ? n : 100)
            .DefaultIfEmpty(100)
            .Max();
        return $"EVT-{max + 1}";
    }

    public void SaveDeals()
    {
        var json = JsonSerializer.Serialize(Deals, JsonOptions);
        File.WriteAllText(_dealsFilePath, json);
    }

    public void SaveCampaignEvents()
    {
        var json = JsonSerializer.Serialize(CampaignEvents, JsonOptions);
        File.WriteAllText(_eventsFilePath, json);
    }

    public void SavePlaybooks()
    {
        var json = JsonSerializer.Serialize(Playbooks, JsonOptions);
        File.WriteAllText(_playbooksFilePath, json);
    }

    /// <summary>Re-seed playbooks to the built-in defaults (used by the Settings "reset" action).</summary>
    public void ResetPlaybooks()
    {
        Playbooks = CreateSeedPlaybooks();
        SavePlaybooks();
    }

    // ---------------- Snapshot / Email settings ----------------

    public void SaveSnapshotSettings()
    {
        SnapshotSettings.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC";
        File.WriteAllText(_snapshotSettingsFilePath, JsonSerializer.Serialize(SnapshotSettings, JsonOptions));
    }

    public void ResetSnapshotSettings()
    {
        SnapshotSettings = CreateSeedSnapshotSettings();
        SaveSnapshotSettings();
    }

    /// <summary>Apply incoming settings, normalised onto the canonical section/field structure, and persist.</summary>
    public void ApplySnapshotSettings(SnapshotSettings incoming)
    {
        SnapshotSettings = MergeSnapshotSettings(incoming);
        SaveSnapshotSettings();
    }

    // ---------------- Approval rules settings ----------------

    public void SaveApprovalRules()
    {
        ApprovalRulesSettings.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC";
        File.WriteAllText(_approvalRulesFilePath, JsonSerializer.Serialize(ApprovalRulesSettings, JsonOptions));
    }

    public void ResetApprovalRules()
    {
        ApprovalRulesSettings = CreateSeedApprovalRules();
        SaveApprovalRules();
    }

    /// <summary>Apply incoming approval rules (preserving the canonical rule ids) and persist.</summary>
    public void ApplyApprovalRules(ApprovalRulesSettings incoming)
    {
        ApprovalRulesSettings = MergeApprovalRules(incoming);
        SaveApprovalRules();
    }

    private void LoadApprovalRules()
    {
        if (!File.Exists(_approvalRulesFilePath))
        {
            ApprovalRulesSettings = CreateSeedApprovalRules();
            SaveApprovalRules();
            return;
        }
        try
        {
            var loaded = JsonSerializer.Deserialize<ApprovalRulesSettings>(File.ReadAllText(_approvalRulesFilePath), JsonOptions);
            ApprovalRulesSettings = loaded is null ? CreateSeedApprovalRules() : MergeApprovalRules(loaded);
        }
        catch
        {
            ApprovalRulesSettings = CreateSeedApprovalRules();
            SaveApprovalRules();
        }
    }

    /// <summary>
    /// Merge saved rules onto the canonical seed so new rule ids added in code always appear,
    /// while keeping the user's enabled flag, threshold, assignee, title, and engagement types.
    /// </summary>
    private static ApprovalRulesSettings MergeApprovalRules(ApprovalRulesSettings loaded)
    {
        var merged = CreateSeedApprovalRules();
        foreach (var rule in merged.Rules)
        {
            var lr = loaded.Rules?.FirstOrDefault(r => r.Id == rule.Id);
            if (lr is null) continue;
            if (!string.IsNullOrWhiteSpace(lr.Title)) rule.Title = lr.Title;
            if (!string.IsNullOrWhiteSpace(lr.Assignee)) rule.Assignee = lr.Assignee;
            rule.Enabled = lr.Enabled;
            if (!string.IsNullOrWhiteSpace(lr.ConditionType)) rule.ConditionType = lr.ConditionType;
            rule.Threshold = lr.Threshold;
            rule.EngagementTypes = lr.EngagementTypes ?? [];
        }
        return merged;
    }

    private static ApprovalRulesSettings CreateSeedApprovalRules() => new()
    {
        Rules =
        [
            new()
            {
                Id = "finance", Title = "Finance Review", Assignee = "Sarah Lee", Enabled = true,
                ConditionType = "discountGreaterThan", Threshold = 15m,
                EngagementTypes = ["Private Offer", "Free Trial", "Hackathon", "POC"]
            },
            new()
            {
                Id = "legal", Title = "Legal Review", Assignee = "Michael Chen", Enabled = true,
                ConditionType = "durationMonthsGreaterThan", Threshold = 24m,
                EngagementTypes = ["Private Offer", "Free Trial", "Hackathon", "POC"]
            },
            new()
            {
                Id = "marketplace", Title = "Marketplace Review", Assignee = "Priya Nair", Enabled = true,
                ConditionType = "marketplacePresent", Threshold = 0m,
                EngagementTypes = ["Private Offer", "Free Trial", "Hackathon", "POC"]
            }
        ]
    };

    // ---------------- People (engagement owners) ----------------

    public void SavePeople() =>
        File.WriteAllText(_peopleFilePath, JsonSerializer.Serialize(People, JsonOptions));

    public void ResetPeople()
    {
        People = CreateSeedPeople();
        SavePeople();
    }

    private void LoadPeople()
    {
        if (!File.Exists(_peopleFilePath))
        {
            People = CreateSeedPeople();
            SavePeople();
            return;
        }
        try
        {
            var loaded = JsonSerializer.Deserialize<List<Person>>(File.ReadAllText(_peopleFilePath), JsonOptions);
            People = loaded is { Count: > 0 } ? loaded : CreateSeedPeople();
        }
        catch
        {
            People = CreateSeedPeople();
            SavePeople();
        }
    }

    private static List<Person> CreateSeedPeople() =>
    [
        new() { Id = "PER-1", Name = "Srinivas K", Email = "srinivas.k@saasify.ai", Role = "Deal Desk", Enabled = true },
        new() { Id = "PER-2", Name = "Priya Sharma", Email = "priya.sharma@saasify.ai", Role = "Sales", Enabled = true },
        new() { Id = "PER-3", Name = "Arjun Mehta", Email = "arjun.mehta@saasify.ai", Role = "Sales", Enabled = true },
        new() { Id = "PER-4", Name = "Neha Gupta", Email = "neha.gupta@saasify.ai", Role = "Partner", Enabled = true }
    ];

    // ---------------- Engagement type settings ----------------

    public void SaveEngagementTypes()
    {
        EngagementTypeSettings.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC";
        File.WriteAllText(_engagementTypesFilePath, JsonSerializer.Serialize(EngagementTypeSettings, JsonOptions));
    }

    public void ResetEngagementTypes()
    {
        EngagementTypeSettings = CreateSeedEngagementTypes();
        SaveEngagementTypes();
    }

    /// <summary>Apply incoming engagement-type settings (normalised onto the canonical catalog) and persist.</summary>
    public void ApplyEngagementTypes(EngagementTypeSettings incoming)
    {
        EngagementTypeSettings = MergeEngagementTypes(incoming);
        SaveEngagementTypes();
    }

    private void LoadEngagementTypes()
    {
        if (!File.Exists(_engagementTypesFilePath))
        {
            EngagementTypeSettings = CreateSeedEngagementTypes();
            SaveEngagementTypes();
            return;
        }
        try
        {
            var loaded = JsonSerializer.Deserialize<EngagementTypeSettings>(File.ReadAllText(_engagementTypesFilePath), JsonOptions);
            EngagementTypeSettings = loaded is null ? CreateSeedEngagementTypes() : MergeEngagementTypes(loaded);
        }
        catch
        {
            EngagementTypeSettings = CreateSeedEngagementTypes();
            SaveEngagementTypes();
        }
    }

    /// <summary>
    /// Merge saved engagement types onto the canonical seed so types added in code always appear,
    /// while keeping the user's enabled flag, section visibilities, labels, and requirements.
    /// </summary>
    private static EngagementTypeSettings MergeEngagementTypes(EngagementTypeSettings loaded)
    {
        var merged = CreateSeedEngagementTypes();
        foreach (var t in merged.Types)
        {
            var lt = loaded.Types?.FirstOrDefault(x => string.Equals(x.Type, t.Type, StringComparison.OrdinalIgnoreCase));
            if (lt is null) continue;
            t.Enabled = lt.Enabled;
            if (!string.IsNullOrWhiteSpace(lt.Blurb)) t.Blurb = lt.Blurb;
            if (!string.IsNullOrWhiteSpace(lt.Products)) t.Products = lt.Products;
            if (!string.IsNullOrWhiteSpace(lt.Pricing)) t.Pricing = lt.Pricing;
            if (!string.IsNullOrWhiteSpace(lt.MeetingNotes)) t.MeetingNotes = lt.MeetingNotes;
            if (!string.IsNullOrWhiteSpace(lt.Approvals)) t.Approvals = lt.Approvals;
            if (!string.IsNullOrWhiteSpace(lt.SubmitLabel)) t.SubmitLabel = lt.SubmitLabel;
            if (!string.IsNullOrWhiteSpace(lt.SubmitAction)) t.SubmitAction = lt.SubmitAction;
            t.TagRequired = lt.TagRequired;
            t.MarketplaceRequired = lt.MarketplaceRequired;
        }
        return merged;
    }

    /// <summary>Built-in engagement catalog — mirrors the frontend default flow configuration.</summary>
    private static EngagementTypeSettings CreateSeedEngagementTypes() => new()
    {
        Types =
        [
            new() { Type = "Private Offer",           Blurb = "Marketplace private offer with full pricing & approvals", Enabled = true, Products = "yes",      Pricing = "yes",      MeetingNotes = "yes", Approvals = "yes",      SubmitLabel = "Submit to SaaSify",    SubmitAction = "submit",        TagRequired = false, MarketplaceRequired = true },
            new() { Type = "Free Trial",              Blurb = "Time-boxed trial — no charge until consumption limit",    Enabled = true, Products = "yes",      Pricing = "optional", MeetingNotes = "yes", Approvals = "no",       SubmitLabel = "Submit to SaaSify",    SubmitAction = "submit",        TagRequired = true,  MarketplaceRequired = true },
            new() { Type = "Workshop",                Blurb = "Customer enablement workshop",                            Enabled = true, Products = "optional", Pricing = "no",       MeetingNotes = "yes", Approvals = "no",       SubmitLabel = "Mark Completed",       SubmitAction = "complete",      TagRequired = true,  MarketplaceRequired = true },
            new() { Type = "Hackathon",               Blurb = "Hands-on hackathon engagement",                           Enabled = true, Products = "yes",      Pricing = "optional", MeetingNotes = "yes", Approvals = "optional", SubmitLabel = "Mark Completed",       SubmitAction = "complete",      TagRequired = true,  MarketplaceRequired = true },
            new() { Type = "POC",                     Blurb = "Proof of concept / pilot",                                Enabled = true, Products = "yes",      Pricing = "optional", MeetingNotes = "yes", Approvals = "optional", SubmitLabel = "Mark Completed",       SubmitAction = "complete",      TagRequired = true,  MarketplaceRequired = true },
            new() { Type = "Summit/Event Lead",       Blurb = "Lead captured at a summit or event",                      Enabled = true, Products = "no",       Pricing = "no",       MeetingNotes = "yes", Approvals = "no",       SubmitLabel = "Save & Convert Later", SubmitAction = "convert-later", TagRequired = true,  MarketplaceRequired = true },
            new() { Type = "Internal Sales Activity", Blurb = "Internal sales activity (no marketplace offer)",          Enabled = true, Products = "no",       Pricing = "no",       MeetingNotes = "yes", Approvals = "no",       SubmitLabel = "Save & Convert Later", SubmitAction = "convert-later", TagRequired = false, MarketplaceRequired = false },
            new() { Type = "External Source Lead",    Blurb = "Lead from an external source",                            Enabled = true, Products = "no",       Pricing = "no",       MeetingNotes = "yes", Approvals = "no",       SubmitLabel = "Save & Convert Later", SubmitAction = "convert-later", TagRequired = false, MarketplaceRequired = false }
        ]
    };

    // ---------------- Home / dashboard settings ----------------

    public void SaveHomeSettings()
    {
        HomeSettings.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC";
        File.WriteAllText(_homeSettingsFilePath, JsonSerializer.Serialize(HomeSettings, JsonOptions));
    }

    public void ResetHomeSettings()
    {
        HomeSettings = CreateSeedHomeSettings();
        SaveHomeSettings();
    }

    /// <summary>Apply incoming home settings (normalised onto the canonical card list) and persist.</summary>
    public void ApplyHomeSettings(HomeSettings incoming)
    {
        HomeSettings = MergeHomeSettings(incoming);
        SaveHomeSettings();
    }

    private void LoadHomeSettings()
    {
        if (!File.Exists(_homeSettingsFilePath))
        {
            HomeSettings = CreateSeedHomeSettings();
            SaveHomeSettings();
            return;
        }
        try
        {
            var loaded = JsonSerializer.Deserialize<HomeSettings>(File.ReadAllText(_homeSettingsFilePath), JsonOptions);
            HomeSettings = loaded is null ? CreateSeedHomeSettings() : MergeHomeSettings(loaded);
        }
        catch
        {
            HomeSettings = CreateSeedHomeSettings();
            SaveHomeSettings();
        }
    }

    /// <summary>
    /// Merge saved cards onto the canonical seed so cards added in code always appear, while
    /// keeping the user's enabled flags (and any custom label).
    /// </summary>
    private static HomeSettings MergeHomeSettings(HomeSettings loaded)
    {
        var merged = CreateSeedHomeSettings();
        foreach (var card in merged.Cards)
        {
            var lc = loaded.Cards?.FirstOrDefault(c => c.Key == card.Key);
            if (lc is null) continue;
            card.Enabled = lc.Enabled;
            if (!string.IsNullOrWhiteSpace(lc.Label)) card.Label = lc.Label;
        }
        return merged;
    }

    private static HomeSettings CreateSeedHomeSettings() => new()
    {
        Cards =
        [
            new() { Key = "stats",          Label = "Summary Stats",      Description = "Top KPI tiles — open engagements, approvals pending, offers submitted, pipeline value.", Enabled = true },
            new() { Key = "insights",       Label = "Engagement Insights", Description = "Active events, engagements, follow-ups, and pending approvals, with leadership snapshot.", Enabled = true },
            new() { Key = "tags",           Label = "Campaign / Event Tags", Description = "Quick tiles for each campaign / event tag and its engagement count.", Enabled = true },
            new() { Key = "openEngagements", Label = "My Open Engagements", Description = "Table of your open engagements with sorting and quick continue.", Enabled = true },
            new() { Key = "recentActivity", Label = "Recent Activity",    Description = "Latest changes across your engagements.", Enabled = true },
            new() { Key = "tasks",          Label = "My Tasks",           Description = "Your action items, list or grouped by engagement.", Enabled = true },
            new() { Key = "reminders",      Label = "Today's Reminders",  Description = "Upcoming and overdue reminders.", Enabled = true }
        ]
    };

    // ---------------- Attention / alert settings ----------------

    public void SaveAttentionSettings()
    {
        AttentionSettings.UpdatedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm") + " UTC";
        File.WriteAllText(_attentionSettingsFilePath, JsonSerializer.Serialize(AttentionSettings, JsonOptions));
    }

    public void ResetAttentionSettings()
    {
        AttentionSettings = new AttentionSettings();
        SaveAttentionSettings();
    }

    public void ApplyAttentionSettings(AttentionSettings incoming)
    {
        AttentionSettings = new AttentionSettings
        {
            AlertEnabled = incoming.AlertEnabled,
            UpcomingEnabled = incoming.UpcomingEnabled,
            UpcomingWindowDays = Math.Clamp(incoming.UpcomingWindowDays <= 0 ? 7 : incoming.UpcomingWindowDays, 1, 30),
            IncludeTasks = incoming.IncludeTasks,
            IncludeReminders = incoming.IncludeReminders,
            IncludeEngagements = incoming.IncludeEngagements
        };
        SaveAttentionSettings();
    }

    private void LoadAttentionSettings()
    {
        if (!File.Exists(_attentionSettingsFilePath))
        {
            AttentionSettings = new AttentionSettings();
            SaveAttentionSettings();
            return;
        }
        try
        {
            var loaded = JsonSerializer.Deserialize<AttentionSettings>(File.ReadAllText(_attentionSettingsFilePath), JsonOptions);
            AttentionSettings = loaded ?? new AttentionSettings();
            if (AttentionSettings.UpcomingWindowDays <= 0) AttentionSettings.UpcomingWindowDays = 7;
        }
        catch
        {
            AttentionSettings = new AttentionSettings();
            SaveAttentionSettings();
        }
    }

    // ---------------- Global audit log ----------------

    /// <summary>Append an audit entry (newest first) and persist, trimming to the most recent entries.</summary>
    public void AppendAudit(AuditEntry entry)
    {
        AuditLog.Insert(0, entry);
        if (AuditLog.Count > MaxAuditEntries)
            AuditLog.RemoveRange(MaxAuditEntries, AuditLog.Count - MaxAuditEntries);
        SaveAuditLog();
    }

    public void SaveAuditLog() =>
        File.WriteAllText(_auditLogFilePath, JsonSerializer.Serialize(AuditLog, JsonOptions));

    private void LoadAuditLog()
    {
        if (!File.Exists(_auditLogFilePath))
        {
            AuditLog = [];
            return;
        }
        try
        {
            var loaded = JsonSerializer.Deserialize<List<AuditEntry>>(File.ReadAllText(_auditLogFilePath), JsonOptions);
            AuditLog = loaded ?? [];
        }
        catch
        {
            AuditLog = [];
        }
    }

    private void LoadSnapshotSettings()
    {
        if (!File.Exists(_snapshotSettingsFilePath))
        {
            SnapshotSettings = CreateSeedSnapshotSettings();
            SaveSnapshotSettings();
            return;
        }

        try
        {
            var json = File.ReadAllText(_snapshotSettingsFilePath);
            var loaded = JsonSerializer.Deserialize<SnapshotSettings>(json, JsonOptions);
            // Merge onto the canonical structure so new sections/fields added in code always appear,
            // while keeping the user's toggles and custom labels.
            SnapshotSettings = loaded is null ? CreateSeedSnapshotSettings() : MergeSnapshotSettings(loaded);
        }
        catch
        {
            SnapshotSettings = CreateSeedSnapshotSettings();
            SaveSnapshotSettings();
        }
    }

    private static SnapshotSettings MergeSnapshotSettings(SnapshotSettings loaded)
    {
        var merged = CreateSeedSnapshotSettings();
        merged.SnapshotButtonEnabled = loaded.SnapshotButtonEnabled;
        merged.EmailButtonEnabled = loaded.EmailButtonEnabled;
        merged.EmailIntro = loaded.EmailIntro ?? "";
        merged.EmailFooter = loaded.EmailFooter ?? "";

        foreach (var section in merged.Sections)
        {
            var ls = loaded.Sections?.FirstOrDefault(s => s.Key == section.Key);
            if (ls is null) continue;
            if (!string.IsNullOrWhiteSpace(ls.Title)) section.Title = ls.Title;
            section.Enabled = ls.Enabled;
            section.InEmail = ls.InEmail;
            foreach (var field in section.Fields)
            {
                var lf = ls.Fields?.FirstOrDefault(f => f.Key == field.Key);
                if (lf is null) continue;
                if (!string.IsNullOrWhiteSpace(lf.Label)) field.Label = lf.Label;
                field.Enabled = lf.Enabled;
            }
        }
        return merged;
    }

    private static SnapshotSettings CreateSeedSnapshotSettings() => new()
    {
        SnapshotButtonEnabled = true,
        EmailButtonEnabled = true,
        EmailIntro = "Here is the latest engagement summary.",
        EmailFooter = "Generated by Marketplace Copilot.",
        Sections =
        [
            new()
            {
                Key = "eventInfo", Title = "Event Information", Enabled = true, InEmail = true,
                Fields =
                [
                    new() { Key = "name", Label = "Event Name", Enabled = true },
                    new() { Key = "dates", Label = "Dates", Enabled = true },
                    new() { Key = "status", Label = "Status", Enabled = true }
                ]
            },
            new()
            {
                Key = "engagementSummary", Title = "Engagement Summary", Enabled = true, InEmail = true,
                Fields =
                [
                    new() { Key = "total", Label = "Total Engagements", Enabled = true },
                    new() { Key = "byType", Label = "Breakdown by type", Enabled = true }
                ]
            },
            new()
            {
                Key = "pipelineSummary", Title = "Pipeline Summary", Enabled = true, InEmail = true,
                Fields =
                [
                    new() { Key = "expectedPipeline", Label = "Expected Pipeline", Enabled = true },
                    new() { Key = "activePrivateOffers", Label = "Active Private Offers", Enabled = true }
                ]
            },
            new()
            {
                Key = "attention", Title = "Engagements Requiring Attention", Enabled = true, InEmail = true,
                Fields =
                [
                    new() { Key = "customer", Label = "Customer", Enabled = true },
                    new() { Key = "engagementType", Label = "Type", Enabled = true },
                    new() { Key = "owner", Label = "Owner", Enabled = true },
                    new() { Key = "status", Label = "Status", Enabled = true },
                    new() { Key = "nextActionDate", Label = "Next Action", Enabled = true },
                    new() { Key = "link", Label = "Open", Enabled = true }
                ]
            },
            new()
            {
                Key = "privateOffers", Title = "Active Private Offers", Enabled = true, InEmail = true,
                Fields =
                [
                    new() { Key = "customer", Label = "Customer", Enabled = true },
                    new() { Key = "marketplace", Label = "Marketplace", Enabled = true },
                    new() { Key = "offerValue", Label = "Offer Value", Enabled = true },
                    new() { Key = "status", Label = "Status", Enabled = true },
                    new() { Key = "expectedCloseDate", Label = "Expected Close", Enabled = true },
                    new() { Key = "link", Label = "Open", Enabled = true }
                ]
            }
        ]
    };

    private void LoadPlaybooks()
    {
        if (!File.Exists(_playbooksFilePath))
        {
            Playbooks = CreateSeedPlaybooks();
            SavePlaybooks();
            return;
        }

        try
        {
            var json = File.ReadAllText(_playbooksFilePath);
            var loaded = JsonSerializer.Deserialize<List<EngagementPlaybook>>(json, JsonOptions);
            Playbooks = loaded is { Count: > 0 } ? loaded : CreateSeedPlaybooks();
        }
        catch
        {
            Playbooks = CreateSeedPlaybooks();
            SavePlaybooks();
        }
    }

    private static List<EngagementPlaybook> CreateSeedPlaybooks() =>
    [
        new()
        {
            EngagementType = "Private Offer",
            Headline = "Private Offer — path to a signed deal",
            NextSteps = ["Confirm products & quantities", "Configure pricing & discount", "Run Finance / Legal / Marketplace approvals", "Generate EULA & private offer", "Buyer accepts in the marketplace"],
            TalkingPoints =
            [
                "We'll send a private offer straight to your marketplace billing account.",
                "Once approved, pricing is locked for the full contract term.",
                "Typical internal approval turnaround is 3–5 business days."
            ],
            Timeline = "Usually 1–3 weeks from today to an accepted offer."
        },
        new()
        {
            EngagementType = "Free Trial",
            Headline = "Free Trial — quick, no-cost activation",
            NextSteps = ["Pick trial length (14 / 30 days)", "Light marketplace review", "Activate trial — no charge", "Track usage during the trial", "Convert to a private offer before it expires"],
            TalkingPoints =
            [
                "There's no cost during the trial — you're only charged if you exceed the included usage, per the EULA.",
                "We can usually activate within 1–2 business days.",
                "We'll check in before the trial ends to plan the conversion."
            ],
            Timeline = "Active within ~2 days; trial runs 14–30 days."
        },
        new()
        {
            EngagementType = "Workshop",
            Headline = "Workshop — hands-on enablement",
            NextSteps = ["Agree scope & audience", "Schedule the session", "Run the workshop", "Capture outcomes & follow-ups", "Mark completed"],
            TalkingPoints =
            [
                "This is a hands-on enablement session for your team — no marketplace pricing involved.",
                "We'll capture action items and suggest the best next engagement afterward."
            ],
            Timeline = "Typically scheduled within 1–2 weeks."
        },
        new()
        {
            EngagementType = "Hackathon",
            Headline = "Hackathon — build with your team",
            NextSteps = ["Define the challenge & products", "Provision environments", "Run the hackathon", "Review outcomes (optional approvals)", "Mark completed / plan next steps"],
            TalkingPoints =
            [
                "Your teams build on our products against a real challenge.",
                "A great precursor to a POC or a private offer.",
                "We can optionally attach products and a light approval if budget is involved."
            ],
            Timeline = "1–4 weeks depending on prep."
        },
        new()
        {
            EngagementType = "POC",
            Headline = "POC — prove the value",
            NextSteps = ["Agree success criteria", "Select products", "Set scope & optional pricing/approvals", "Run the POC", "Review results → convert to an offer"],
            TalkingPoints =
            [
                "We'll define clear, measurable success criteria up front.",
                "It's a scoped, time-boxed evaluation on your own data.",
                "On success, we move straight to a private offer."
            ],
            Timeline = "Typically 2–6 weeks."
        },
        new()
        {
            EngagementType = "Summit/Event Lead",
            Headline = "Summit / Event Lead — capture & nurture",
            NextSteps = ["Capture contact & interest", "Tag to the event", "Qualify the lead", "Save & convert later", "Convert to a POC / Private Offer when ready"],
            TalkingPoints =
            [
                "Great to connect at the event — there's no commitment right now.",
                "We'll follow up with next steps tailored to your needs."
            ],
            Timeline = "Follow-up within a few days."
        },
        new()
        {
            EngagementType = "Internal Sales Activity",
            Headline = "Internal Sales Activity — track the motion",
            NextSteps = ["Log the activity", "Link related accounts", "Plan follow-up", "Convert to a customer engagement when relevant"],
            TalkingPoints = ["Internal tracking only — no marketplace offer is created."],
            Timeline = "N/A — internal."
        },
        new()
        {
            EngagementType = "External Source Lead",
            Headline = "External Source Lead — qualify the source",
            NextSteps = ["Record source & contact", "Qualify fit", "Assign an owner", "Convert to an engagement once qualified"],
            TalkingPoints = ["Lead sourced externally — qualify fit before investing.", "We'll route it to the right owner and follow up."],
            Timeline = "Qualify within a week."
        }
    ];

    private void LoadCampaignEvents()
    {
        if (!File.Exists(_eventsFilePath))
        {
            CampaignEvents = CreateSeedEvents();
            SaveCampaignEvents();
            return;
        }

        try
        {
            var json = File.ReadAllText(_eventsFilePath);
            var loaded = JsonSerializer.Deserialize<List<CampaignEvent>>(json, JsonOptions);
            CampaignEvents = loaded is { Count: > 0 } ? loaded : CreateSeedEvents();
        }
        catch
        {
            CampaignEvents = CreateSeedEvents();
            SaveCampaignEvents();
        }
    }

    private static List<CampaignEvent> CreateSeedEvents()
    {
        var today = DateTime.UtcNow.Date;
        return
        [
            new()
            {
                Id = "EVT-101",
                Name = "AWS Summit 2026",
                Marketplace = "AWS",
                StartDate = today.AddDays(11).ToString("yyyy-MM-dd"),
                EndDate = today.AddDays(15).ToString("yyyy-MM-dd"),
                Description = "Flagship AWS event for cloud and AI workloads.",
                CreatedAt = today.ToString("yyyy-MM-dd")
            },
            new()
            {
                Id = "EVT-102",
                Name = "Microsoft Build 2026",
                Marketplace = "Azure",
                StartDate = today.AddDays(-2).ToString("yyyy-MM-dd"),
                EndDate = today.AddDays(2).ToString("yyyy-MM-dd"),
                Description = "Developer conference for Azure and Microsoft 365.",
                CreatedAt = today.ToString("yyyy-MM-dd")
            },
            new()
            {
                Id = "EVT-103",
                Name = "Google Cloud Next '26",
                Marketplace = "GCP",
                StartDate = today.AddDays(-40).ToString("yyyy-MM-dd"),
                EndDate = today.AddDays(-36).ToString("yyyy-MM-dd"),
                Description = "Google Cloud's annual conference (already concluded).",
                CreatedAt = today.AddDays(-60).ToString("yyyy-MM-dd")
            }
        ];
    }

    private void LoadDeals()
    {
        if (!File.Exists(_dealsFilePath))
        {
            Deals = CreateSeedDeals();
            EnsureCreatedDates();
            SaveDeals();
            return;
        }

        try
        {
            var json = File.ReadAllText(_dealsFilePath);
            var loaded = JsonSerializer.Deserialize<List<Deal>>(json, JsonOptions);
            Deals = loaded is { Count: > 0 } ? loaded : CreateSeedDeals();
            EnsureCreatedDates();
        }
        catch
        {
            Deals = CreateSeedDeals();
            EnsureCreatedDates();
            SaveDeals();
        }
    }

    private static List<Deal> CreateSeedDeals() =>
    [
        new()
        {
            Id = "DL-1001",
            Name = "Infosys Enterprise Deal",
            Customer = "Infosys Ltd.",
            ContactName = "Rajesh Kumar",
            ContactEmail = "rajesh@infosys.com",
            Marketplace = "Azure",
            DealType = "New Deal",
            ExpectedValue = 321300,
            Stage = "Pricing",
            StepNumber = 3,
            TotalSteps = 5,
            MarketplaceStatus = "In Review",
            Owner = "Srinivas K",
            LastUpdated = "Today, 10:30 AM",
            ProductIds = ["prod-1"],
            Pricing = new PricingConfig
            {
                ContractStart = "Jul 01, 2026",
                ContractEnd = "Jun 30, 2029",
                DurationMonths = 36,
                DiscountPercent = 15,
                PublicContractValue = 360000,
                TotalDiscount = 54000,
                NetPriceBeforeFees = 306000,
                MarketplaceFee = 15300,
                NetContractValue = 321300,
                TotalPayable = 321300
            }
        },
        new()
        {
            Id = "DL-1002",
            Name = "TCS Cloud Migration",
            Customer = "Tata Consultancy Services",
            Marketplace = "AWS",
            ExpectedValue = 280000,
            Stage = "Approval",
            StepNumber = 5,
            MarketplaceStatus = "Waiting for Info",
            Owner = "Srinivas K",
            LastUpdated = "Yesterday, 4:15 PM"
        },
        new()
        {
            Id = "DL-1003",
            Name = "Wipro Cloud Transformation",
            Customer = "Wipro Ltd.",
            Marketplace = "GCP",
            ExpectedValue = 195000,
            Stage = "Discovery",
            StepNumber = 2,
            MarketplaceStatus = "Draft",
            Owner = "Srinivas K",
            LastUpdated = "2 days ago"
        }
    ];

    private void EnsureCreatedDates()
    {
        var maxNum = Deals
            .Select(d => ParseDealNumber(d.Id))
            .DefaultIfEmpty(1000)
            .Max();

        var changed = false;
        foreach (var deal in Deals)
        {
            if (!string.IsNullOrWhiteSpace(deal.CreatedAt)) continue;
            var num = ParseDealNumber(deal.Id);
            var daysAgo = Math.Max(0, maxNum - num);
            deal.CreatedAt = DateTime.UtcNow.Date.AddDays(-daysAgo).ToString("yyyy-MM-dd");
            changed = true;
        }

        if (changed) SaveDeals();
    }

    private static int ParseDealNumber(string id)
    {
        var n = id.Replace("DL-", "", StringComparison.OrdinalIgnoreCase);
        return int.TryParse(n, out var num) ? num : 1000;
    }
}
