namespace LoanManagement.DL.Models;

public class DailyCollectionRecord
{
    public string LoanAccountNo { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public decimal EMIAmount { get; set; }

    public decimal PenaltyAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CollectionDate { get; set; }

    public int InstallmentNumber { get; set; }
}
