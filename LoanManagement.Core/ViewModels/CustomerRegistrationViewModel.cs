
using System.ComponentModel.DataAnnotations;

namespace LoanManagement.Core.ViewModels;

public class CustomerRegistrationViewModel
{
    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(300)]
    [Display(Name = "Customer Name")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Aadhaar number is required.")]
    [RegularExpression(@"^\d{12}$",
        ErrorMessage = "Enter a valid 12-digit Aadhaar number.")]
    [Display(Name = "Aadhaar Number")]
    public string AadhaarNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "PAN Number is required.")]
    [RegularExpression(@"^[A-Za-z]{5}[0-9]{4}[A-Za-z]$",
        ErrorMessage = "Enter a valid PAN Number.")]
    [Display(Name = "PAN Number")]
    public string PANNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer photo is required.")]
    [Display(Name = "Customer Photo")]
    public IFormFile? Photo { get; set; }

    [Required(ErrorMessage = "Signature image is required.")]
    [Display(Name = "Signature Image")]
    public IFormFile? SignatureImage { get; set; }

    [Required(ErrorMessage = "Aadhaar document is required.")]
    [Display(Name = "Aadhaar Document")]
    public IFormFile? AadhaarDocument { get; set; }

    [Required(ErrorMessage = "PAN document is required.")]
    [Display(Name = "PAN Document")]
    public IFormFile? PANDocument { get; set; }

    [Required(ErrorMessage = "Referred person is required.")]
    [StringLength(300)]
    [Display(Name = "Referred Person")]
    public string ReferredPerson { get; set; } = string.Empty;

    [Required(ErrorMessage = "Referred contact number is required.")]
    [RegularExpression(@"^\d{10}$",
        ErrorMessage = "Enter a valid 10-digit referred contact number.")]
    [Display(Name = "Referred Contact Number")]
    public string ReferredContactNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Primary contact number is required.")]
    [RegularExpression(@"^\d{10}$",
        ErrorMessage = "Enter a valid 10-digit primary contact number.")]
    [Display(Name = "Primary Contact Number")]
    public string PrimaryContactNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Secondary contact number is required.")]
    [RegularExpression(@"^\d{10}$",
        ErrorMessage = "Enter a valid 10-digit secondary contact number.")]
    [Display(Name = "Secondary Contact Number")]
    public string SecondaryContactNo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Loan amount is required.")]
    [Range(5000, 50000,
        ErrorMessage = "Loan amount must be between ₹5,000 and ₹50,000.")]
    [Display(Name = "Loan Amount")]
    public decimal LoanAmount { get; set; }

    [StringLength(2000)]
    [Display(Name = "Remarks")]
    public string Remarks { get; set; } = string.Empty;
}
