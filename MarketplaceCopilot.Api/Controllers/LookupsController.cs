namespace MarketplaceCopilot.Api.Controllers;

using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class LookupsController(DataStore store) : ControllerBase
{
    [HttpGet]
    public ActionResult<LookupData> Get() => Ok(new LookupData
    {
        Countries =
        [
            "United States", "United Kingdom", "Canada", "India", "Australia",
            "Germany", "France", "Netherlands", "Singapore", "Japan",
            "United Arab Emirates", "Brazil", "Mexico", "Ireland", "Switzerland",
            "Sweden", "Norway", "Denmark", "Finland", "Spain", "Italy",
            "South Korea", "New Zealand", "South Africa", "Israel"
        ],
        Industries =
        [
            "Software & SaaS",
            "Cloud Infrastructure",
            "Cybersecurity",
            "FinTech",
            "HealthTech",
            "EdTech",
            "MarTech",
            "HR Tech",
            "Data & Analytics",
            "AI & Machine Learning",
            "DevOps & Platform Engineering",
            "IT Services & Consulting",
            "E-commerce & Retail Tech",
            "Telecommunications",
            "Manufacturing & Industrial SaaS",
            "Government & Public Sector",
            "Media & Entertainment",
            "Legal Tech",
            "PropTech",
            "InsurTech"
        ],
        DealTypes = ["New Deal", "Renewal"],
        Marketplaces = ["AWS", "Azure", "GCP"],
        // Only types enabled in Settings → Engagement Types are offered when creating an engagement.
        EngagementTypes = store.EngagementTypeSettings.Types.Where(t => t.Enabled).Select(t => t.Type).ToList(),
        DealOwners = ["Srinivas K", "Priya Sharma", "Arjun Mehta", "Neha Gupta"],
        OfferTypes = ["Free Trial", "Direct Private Offer", "Reseller Private Offer", "Renewal"],
        Priorities = ["High", "Medium", "Low"]
    });
}
