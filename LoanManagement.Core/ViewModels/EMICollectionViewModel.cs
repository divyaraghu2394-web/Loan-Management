
using LoanManagement.DL.Models;
using System.ComponentModel.DataAnnotations;

namespace LoanManagement.Core.ViewModels
{
    public class EMICollectionViewModel
    {
        // Loan search
        [Required(ErrorMessage = "Loan Account Number is required.")]
        public string LoanAccountNo { get; set; } = string.Empty;

        // Customer loan details
        public CustomerLoanDetails? Customer { get; set; }

        // Previous EMI collections
        public IEnumerable<EMICollection> Collections { get; set; }
            = new List<EMICollection>();

        // Active customer loans for grid
        public IEnumerable<CustomerLoanDetails> ActiveLoans { get; set; }
            = new List<CustomerLoanDetails>();

        // Collection summary
        public int CollectedInstallments { get; set; }

        public int RemainingInstallments { get; set; }

        public int NextInstallmentNumber { get; set; }

        public bool IsLoanCompleted { get; set; }

        // New EMI collection details
        [Range(1, 10, ErrorMessage = "Select at least one installment.")]
        [Display(Name = "Installments to collect")]
        public int InstallmentsToCollect { get; set; } = 1;

        [Range(0, 999999999)]
        [Display(Name = "Penalty per installment")]
        public decimal? PenaltyAmount { get; set; }

        [Required]
        [Display(Name = "Payment mode")]
        public string PaymentMode { get; set; } = "Cash";

        [StringLength(100)]
        [Display(Name = "Payment reference")]
        public string? PaymentReference { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        public decimal SuggestedPenaltyAmount { get; set; }

        public bool IsPenaltyApplicable { get; set; }

        public decimal TotalCollectedAmount { get; set; }

        public decimal TotalPenaltyCollected { get; set; }

        public decimal OutstandingAmount { get; set; }

        public int DaysOverdue { get; set; }

        public string CollectionStatus { get; set; } = "Active";

        [Required(ErrorMessage = "Collection date is required.")]
        [Display(Name = "Collection Date")]
        public DateTime CollectionDate { get; set; } = DateTime.Today;
    }
}
