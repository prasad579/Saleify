using MarketplaceCopilot.Entities;

namespace MarketplaceCopilot.Services.Contracts;

public interface IPricingService
{
    PricingConfig Calculate(PricingConfig input);
    string BuildInsight(decimal discountPercent, decimal netContractValue);
}
