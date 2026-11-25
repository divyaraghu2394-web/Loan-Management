
namespace LoanManagement.DL.Models
{
    public class EMICollection
    {
        public long CollectionId { get; set; }

        public string LoanAccountNo { get; set; } = string.Empty;

        public decimal EMIAmount { get; set; }

        public decimal PenaltyAmount { get; set; }

        public decimal? TotalAmount { get; set; }

        public DateTime CollectionDate { get; set; }

        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; }

        public int InstallmentNumber { get; set; }

        public string? ReceiptNumber { get; set; }

        public string PaymentMode { get; set; } = "Cash";

        public string? PaymentReference { get; set; }

        public string? Remarks { get; set; }

        public bool IsReversed { get; set; }

        public string? ReversalReason { get; set; }

        public string? ReversedBy { get; set; }

        public DateTime? ReversedOn { get; set; }

        public string? ModifiedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }
    }
}
