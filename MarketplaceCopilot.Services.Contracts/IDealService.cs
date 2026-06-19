using MarketplaceCopilot.Entities;

namespace MarketplaceCopilot.Services.Contracts;

public interface IDealService
{
    List<Product> GetSelectedProducts(Deal deal);
    decimal GetListPriceFromProducts(Deal deal);
    DealDetailDto ToDetail(Deal deal);
}
