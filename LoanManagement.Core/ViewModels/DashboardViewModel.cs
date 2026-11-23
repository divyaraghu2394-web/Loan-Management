using LoanManagement.DL.Models;

namespace LoanManagement.Core.ViewModels;

public class DashboardViewModel
{
    public DateTime BusinessDate { get; set; } = DateTime.Today;

    public string SearchTerm { get; set; } = string.Empty;

    public DailyCollectionSummary DailyCollections { get; set; } = new();

    public IEnumerable<DailyCollectionRecord> DailyCollectionRecords { get; set; }
        = new List<DailyCollectionRecord>();

    public IEnumerable<CustomerLoanDetails> ActiveLoans { get; set; }
        = new List<CustomerLoanDetails>();
}
