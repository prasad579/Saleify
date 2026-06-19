using MarketplaceCopilot.Entities;
using MarketplaceCopilot.Services.Contracts;

namespace MarketplaceCopilot.Services;

public class PricingService : IPricingService
{
    public static decimal GetMarketplaceFeePercent(string marketplace) => marketplace.ToLowerInvariant() switch
    {
        "azure" => 5m,
        "aws" => 3m,
        "gcp" => 5m,
        _ => 5m
    };

    public PricingConfig Calculate(PricingConfig input)
    {
        NormalizeDurationFields(input);
        SyncContractDates(input);

        var durationDays = input.DurationDays > 0 ? input.DurationDays : ComputeDurationDays(input);
        input.DurationDays = durationDays;
        input.DurationMonths = Math.Max(1, (int)Math.Round(durationDays / 30.4375m));

        var durationYears = durationDays / 365.25m;
        var isAbsolute = input.PricingMethod.Equals("Absolute Price", StringComparison.OrdinalIgnoreCase);
        var isPerYear = IsPerYearDiscount(input);

        if (isPerYear)
        {
            CalculatePerYearDiscount(input);
        }
        else if (isAbsolute)
        {
            input.PublicContractValue = Math.Round(input.PublicPricePerYear * durationYears, 2);
            input.NetPriceBeforeFees = input.AbsoluteContractPrice;
            input.TotalDiscount = input.PublicContractValue - input.AbsoluteContractPrice;
            BuildFlatYearlyBreakdown(input, input.DiscountPercent);
        }
        else
        {
            input.PublicContractValue = Math.Round(input.PublicPricePerYear * durationYears, 2);
            input.TotalDiscount = Math.Round(input.PublicContractValue * (input.DiscountPercent / 100m), 2);
            input.NetPriceBeforeFees = input.PublicContractValue - input.TotalDiscount;
            BuildFlatYearlyBreakdown(input, input.DiscountPercent);
        }

        input.MarketplaceFee = Math.Round(input.NetPriceBeforeFees * (input.MarketplaceFeePercent / 100m), 2);
        input.NetContractValue = input.NetPriceBeforeFees + input.MarketplaceFee;
        input.TotalPayable = input.NetContractValue;
        BuildInstallmentSchedule(input);
        return input;
    }

    public string BuildInsight(decimal discountPercent, decimal netContractValue)
    {
        if (discountPercent > 25)
            return $"A {discountPercent}% discount on ${netContractValue:N0} is aggressive — approval and legal review are strongly recommended.";
        if (discountPercent > 15)
            return $"A {discountPercent}% discount is above standard — confirm margin before submitting for approval.";
        if (discountPercent > 0)
            return $"A {discountPercent}% discount on ${netContractValue:N0} is within a typical enterprise range.";
        return "No discount applied — list price will be used for the private offer.";
    }

    private static void NormalizeDurationFields(PricingConfig input)
    {
        if (string.IsNullOrWhiteSpace(input.DurationType))
        {
            if (input.DurationValue <= 0 && input.DurationMonths > 0)
            {
                input.DurationType = input.DurationMonths % 12 == 0 ? "years" : "months";
                input.DurationValue = input.DurationType == "years"
                    ? input.DurationMonths / 12
                    : input.DurationMonths;
            }
            else
            {
                input.DurationType = "years";
                input.DurationValue = input.DurationValue > 0 ? input.DurationValue : 3;
            }
        }

        input.DurationType = input.DurationType.ToLowerInvariant();
        if (input.DurationValue <= 0)
            input.DurationValue = input.DurationType switch
            {
                "days" => 365,
                "months" => 36,
                _ => 3
            };

        if (!IsPerYearDiscount(input) &&
            input.DiscountModel.Contains("Different", StringComparison.OrdinalIgnoreCase) &&
            !input.DurationType.Equals("years", StringComparison.OrdinalIgnoreCase))
        {
            input.DiscountModel = "Same discount for entire contract";
        }
    }

    private static void SyncContractDates(PricingConfig input)
    {
        var start = ParseDate(input.ContractStart);
        var end = ParseDate(input.ContractEnd);

        if (start.HasValue && end.HasValue && end.Value >= start.Value)
        {
            input.DurationDays = (end.Value - start.Value).Days + 1;
            input.DurationValue = input.DurationType switch
            {
                "days" => input.DurationDays,
                "months" => Math.Max(1, (int)Math.Round(input.DurationDays / 30.4375m)),
                _ => Math.Max(1, (int)Math.Round(input.DurationDays / 365.25m))
            };
            return;
        }

        if (start.HasValue && input.DurationValue > 0)
        {
            var computedEnd = AddDuration(start.Value, input.DurationValue, input.DurationType);
            input.ContractEnd = computedEnd.ToString("yyyy-MM-dd");
            input.DurationDays = (computedEnd - start.Value).Days + 1;
            return;
        }

        if (end.HasValue && input.DurationValue > 0)
        {
            var computedStart = SubtractDuration(end.Value, input.DurationValue, input.DurationType);
            input.ContractStart = computedStart.ToString("yyyy-MM-dd");
            input.DurationDays = (end.Value - computedStart).Days + 1;
        }
    }

    private static int ComputeDurationDays(PricingConfig input) => input.DurationType switch
    {
        "days" => input.DurationValue,
        "months" => (int)Math.Round(input.DurationValue * 30.4375m),
        _ => (int)Math.Round(input.DurationValue * 365.25m)
    };

    private static bool IsPerYearDiscount(PricingConfig input) =>
        input.PricingMethod.Equals("Discount Based", StringComparison.OrdinalIgnoreCase) &&
        input.DiscountModel.Contains("Different", StringComparison.OrdinalIgnoreCase) &&
        input.DurationType.Equals("years", StringComparison.OrdinalIgnoreCase) &&
        input.DurationValue >= 1;

    private static void CalculatePerYearDiscount(PricingConfig input)
    {
        input.YearlyBreakdown.Clear();
        decimal publicTotal = 0;
        decimal discountTotal = 0;
        decimal netTotal = 0;

        for (var year = 1; year <= input.DurationValue; year++)
        {
            var list = input.PublicPricePerYear;
            var pct = GetYearDiscountPercent(input, year);
            var disc = Math.Round(list * (pct / 100m), 2);
            var net = list - disc;
            publicTotal += list;
            discountTotal += disc;
            netTotal += net;
            input.YearlyBreakdown.Add(new YearlyPricingRow
            {
                Year = year,
                Period = $"Year {year}",
                ListPrice = list,
                DiscountPercent = pct,
                DiscountAmount = disc,
                YourPrice = net
            });
        }

        input.PublicContractValue = publicTotal;
        input.TotalDiscount = discountTotal;
        input.NetPriceBeforeFees = netTotal;
    }

    private static void BuildFlatYearlyBreakdown(PricingConfig input, decimal discountPercent)
    {
        input.YearlyBreakdown.Clear();
        var rows = Math.Max(1, (int)Math.Ceiling(input.DurationDays / 365.25m));
        for (var year = 1; year <= rows; year++)
        {
            var list = input.PublicPricePerYear;
            var disc = Math.Round(list * (discountPercent / 100m), 2);
            input.YearlyBreakdown.Add(new YearlyPricingRow
            {
                Year = year,
                Period = rows == 1 ? "Contract period" : $"Year {year}",
                ListPrice = list,
                DiscountPercent = discountPercent,
                DiscountAmount = disc,
                YourPrice = list - disc
            });
        }
    }

    private static decimal GetYearDiscountPercent(PricingConfig input, int year)
    {
        if (input.YearlyDiscountPercents.Count >= year)
            return input.YearlyDiscountPercents[year - 1];
        return input.DiscountPercent;
    }

    private static void BuildInstallmentSchedule(PricingConfig input)
    {
        input.InstallmentSchedule.Clear();
        if (!input.FlexiblePaymentsEnabled || input.InstallmentCount < 2)
            return;

        var count = Math.Max(2, input.InstallmentCount);
        var amountEach = Math.Round(input.TotalPayable / count, 2);
        var remainder = input.TotalPayable - (amountEach * count);
        var start = ParseDate(input.ContractStart) ?? DateTime.UtcNow.Date;

        for (var i = 0; i < count; i++)
        {
            var due = start.AddMonths(i);
            var amount = i == count - 1 ? amountEach + remainder : amountEach;
            input.InstallmentSchedule.Add(new InstallmentRow
            {
                Number = i + 1,
                DueDate = due.ToString("yyyy-MM-dd"),
                Amount = amount
            });
        }
    }

    private static DateTime AddDuration(DateTime start, int value, string type) => type switch
    {
        "days" => start.AddDays(value - 1),
        "months" => start.AddMonths(value).AddDays(-1),
        _ => start.AddYears(value).AddDays(-1)
    };

    private static DateTime SubtractDuration(DateTime end, int value, string type) => type switch
    {
        "days" => end.AddDays(-(value - 1)),
        "months" => end.AddMonths(-value).AddDays(1),
        _ => end.AddYears(-value).AddDays(1)
    };

    private static DateTime? ParseDate(string value) =>
        DateTime.TryParse(value, out var dt) ? dt.Date : null;
}
