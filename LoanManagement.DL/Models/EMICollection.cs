
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
    }
}
