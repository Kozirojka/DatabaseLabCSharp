namespace Wpf_db_008_0._2v;

public class UsageMonitorData
{
    public int CustomerID { get; set; }
    public string CustomerName { get; set; }
    public string TariffName { get; set; }
    public int TariffSpeed { get; set; }
    public decimal TotalDataUsedGB { get; set; }
    public decimal ExcessDataGB { get; set; }
    public int DaysActive { get; set; }
    public decimal AvgDailyUsageGB { get; set; }
    public string SubscriptionStatus { get; set; }
}
