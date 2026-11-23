namespace LoanManagement.DL.Models;

public class Customer
{
    public string LoanAccountNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string AadhaarNumber { get; set; } = string.Empty;
    public string PANNumber { get; set; } = string.Empty;
    public string? SignatureImagePath { get; set; }
    public string? PhotoPath { get; set; }
    public string? AadhaarDocumentPath { get; set; }
    public string? PANDocumentPath { get; set; }
    public string? ReferredPerson { get; set; }
    public string? ReferredContactNo { get; set; }
    public string PrimaryContactNo { get; set; } = string.Empty;
    public string SecondaryContactNo { get; set; } = string.Empty;
    public decimal LoanAmount { get; set; }
    public decimal EMIAmount { get; set; }
    public int InstallmentCount { get; set; }
    public bool IsLive { get; set; } = true;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedOn { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? Remarks { get; set; }
}