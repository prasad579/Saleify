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

    public List<Deal> Deals { get; private set; } = [];

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
        LoadDeals();
    }

    public string NextDealId()
    {
        var max = Deals
            .Select(d => int.TryParse(d.Id.Replace("DL-", "", StringComparison.OrdinalIgnoreCase), out var n) ? n : 1000)
            .DefaultIfEmpty(1000)
            .Max();
        return $"DL-{max + 1}";
    }

    public void SaveDeals()
    {
        var json = JsonSerializer.Serialize(Deals, JsonOptions);
        File.WriteAllText(_dealsFilePath, json);
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
