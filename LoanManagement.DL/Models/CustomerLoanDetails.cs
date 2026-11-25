
namespace LoanManagement.DL.Models
{
    public class CustomerLoanDetails
    {
        public string LoanAccountNo { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public decimal LoanAmount { get; set; }

        public decimal EMIAmount { get; set; }

        public int InstallmentCount { get; set; }

        public bool IsLive { get; set; }

        public string? ReferredPerson { get; set; }

        public string? PrimaryContactNo { get; set; }

        public string? PhotoPath { get; set; }

        public DateTime LoanCreatedOn { get; set; }

        public DateTime? LastCollectionDate { get; set; }

        public DateTime? NextEMIDate { get; set; }

        public List<DateTime> UpcomingEMIDates { get; set; } = new();

        // EMI Collection Summary
        public int CollectedInstallments { get; set; }

        public int RemainingInstallments { get; set; }

        public decimal PendingAmount { get; set; }

        public int DaysOverdue { get; set; }

        public string CollectionStatus { get; set; } = "Active";
    }
}
