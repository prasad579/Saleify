using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;

namespace MarketplaceCopilot.Services;

public class DealService(DataStore store, IPricingService pricingService) : IDealService
{
    public List<Product> GetSelectedProducts(Deal deal) =>
        store.Products.Where(p => deal.ProductIds.Contains(p.Id)).ToList();

    public decimal GetListPriceFromProducts(Deal deal) =>
        GetSelectedProducts(deal).Sum(p => p.ListPricePerYear);

    public DealDetailDto ToDetail(Deal deal)
    {
        var products = GetSelectedProducts(deal);
        var listPrice = products.Sum(p => p.ListPricePerYear);
        var feePercent = PricingService.GetMarketplaceFeePercent(deal.Marketplace);

        if (deal.Pricing is null && listPrice > 0)
        {
            deal.Pricing = new PricingConfig
            {
                PublicPricePerYear = listPrice,
                MarketplaceFeePercent = feePercent,
                DurationMonths = 36,
                DiscountPercent = 0
            };
            pricingService.Calculate(deal.Pricing);
        }
        else if (deal.Pricing is not null && deal.Pricing.PublicPricePerYear == 0 && listPrice > 0)
        {
            deal.Pricing.PublicPricePerYear = listPrice;
            deal.Pricing.MarketplaceFeePercent = feePercent;
            pricingService.Calculate(deal.Pricing);
        }

        return new DealDetailDto
        {
            Deal = deal,
            SelectedProducts = products,
            SuggestedPublicPricePerYear = listPrice,
            MarketplaceFeePercent = feePercent,
            PricingInsight = deal.Pricing is not null
                ? pricingService.BuildInsight(deal.Pricing.DiscountPercent, deal.Pricing.NetContractValue)
                : "Select products to calculate pricing."
        };
    }
}
