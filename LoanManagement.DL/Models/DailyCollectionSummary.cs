namespace LoanManagement.DL.Models;

public class DailyCollectionSummary
{
    public int CollectionCount { get; set; }

    public decimal EMIAmount { get; set; }

    public decimal PenaltyAmount { get; set; }

    public decimal TotalAmount { get; set; }
}
