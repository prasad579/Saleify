namespace MarketplaceCopilot.Entities;

public class DealDetailDto
{
    public Deal Deal { get; set; } = new();
    public List<Product> SelectedProducts { get; set; } = [];
    public decimal SuggestedPublicPricePerYear { get; set; }
    public decimal MarketplaceFeePercent { get; set; }
    public string PricingInsight { get; set; } = "";
}
