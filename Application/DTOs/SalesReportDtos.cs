namespace EShop.Application.DTOs;

public enum ReportPeriod
{
    Today = 0,
    Yesterday = 1,
    Last7Days = 2,
    Last30Days = 3,
    ThisMonth = 4,
    LastMonth = 5,
    Last3Months = 6,
    ThisYear = 7,
    Custom = 8
}

public class SalesReportFilterDto
{
    public ReportPeriod Period { get; set; } = ReportPeriod.Last30Days;
    public DateTime? CustomFrom { get; set; }
    public DateTime? CustomTo { get; set; }

    public (DateTime From, DateTime To) ResolveRange()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        return Period switch
        {
            ReportPeriod.Today => (today, today.AddDays(1).AddTicks(-1)),
            ReportPeriod.Yesterday => (today.AddDays(-1), today.AddTicks(-1)),
            ReportPeriod.Last7Days => (today.AddDays(-6), now),
            ReportPeriod.Last30Days => (today.AddDays(-29), now),
            ReportPeriod.ThisMonth => (new DateTime(now.Year, now.Month, 1), now),
            ReportPeriod.LastMonth => (new DateTime(now.Year, now.Month, 1).AddMonths(-1),
                                       new DateTime(now.Year, now.Month, 1).AddTicks(-1)),
            ReportPeriod.Last3Months => (today.AddMonths(-3), now),
            ReportPeriod.ThisYear => (new DateTime(now.Year, 1, 1), now),
            ReportPeriod.Custom => (CustomFrom ?? today.AddDays(-29), CustomTo ?? now),
            _ => (today.AddDays(-29), now)
        };
    }
}

public class SalesReportDto
{
    public SalesReportFilterDto Filter { get; set; } = new();
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public SalesSummaryDto Summary { get; set; } = new();
    public List<ChartPointDto> SalesTimeline { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
    public List<NameValueDto> OrderStatusBreakdown { get; set; } = new();
    public List<NameValueDto> PaymentMethodBreakdown { get; set; } = new();
    public List<NameValueDto> CategorySales { get; set; } = new();
    public List<NameValueDto> DayOfWeekSales { get; set; } = new();
}

public class SalesSummaryDto
{
    public int TotalOrders { get; set; }
    public int PaidOrders { get; set; }
    public int PendingOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public int UniqueCustomers { get; set; }
    public int NewCustomers { get; set; }
    public int TotalItemsSold { get; set; }
    public int UniqueProductsSold { get; set; }
    public decimal RevenueGrowthPercent { get; set; }
    public decimal OrdersGrowthPercent { get; set; }
    public decimal AovGrowthPercent { get; set; }
}

public class ChartPointDto
{
    public string Label { get; set; } = "";
    public string Key { get; set; } = "";
    public decimal Value { get; set; }
    public int Count { get; set; }
}

public class NameValueDto
{
    public string Name { get; set; } = "";
    public decimal Value { get; set; }
    public int Count { get; set; }
    public string? Color { get; set; }
}

public class TopProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? ImageUrl { get; set; }
    public int QuantitySold { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}

public class TopCustomerDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal AverageOrderValue { get; set; }
    public DateTime LastOrderAt { get; set; }
}