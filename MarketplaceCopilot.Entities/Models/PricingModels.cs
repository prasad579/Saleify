namespace MarketplaceCopilot.Entities;

public class YearlyPricingRow
{
    public int Year { get; set; }
    public string Period { get; set; } = "";
    public decimal ListPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal YourPrice { get; set; }
}

public class InstallmentRow
{
    public int Number { get; set; }
    public string DueDate { get; set; } = "";
    public decimal Amount { get; set; }
}
