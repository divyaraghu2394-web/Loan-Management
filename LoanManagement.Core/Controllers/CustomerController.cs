
using LoanManagement.Core.Services;
using LoanManagement.Core.ViewModels;
using LoanManagement.DL.Interfaces;
using LoanManagement.DL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace LoanManagement.Core.Controllers;

[Authorize(Roles = "Admin")]
public class CustomerController : Controller
{
    private readonly EMICalculationService _emiService;
    private readonly ICustomerRepository _customerRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CustomerController> _logger;
    private readonly IDocumentValidationService _documentValidationService;

    public CustomerController(
        EMICalculationService emiService,
        ICustomerRepository customerRepository,
        IWebHostEnvironment environment,
        ILogger<CustomerController> logger,
        IDocumentValidationService documentValidationService)
    {
        _emiService = emiService;
        _customerRepository = customerRepository;
        _environment = environment;
        _logger = logger;
        _documentValidationService = documentValidationService;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new CustomerRegistrationViewModel());
    }

    [HttpGet]
    public IActionResult GetEMIAmount(decimal loanAmount)
    {
        if (loanAmount <= 0)
            return BadRequest(new
            {
                success = false,
                message = "Enter a valid loan amount."
            });

        var emiAmount = _emiService.GetEMIAmount(loanAmount);

        if (emiAmount == null)
            return Ok(new
            {
                success = false,
                message = "EMI configuration not available for this loan amount."
            });

        var installmentCount = _emiService.GetInstallmentCount();

        return Ok(new
        {
            success = true,
            emiAmount,
            installmentCount,
            totalEMIAmount = emiAmount.Value * installmentCount
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckCustomer(
        string customerName,
        string aadhaarNumber,
        string panNumber)
    {
        customerName = customerName?.Trim() ?? string.Empty;
        aadhaarNumber = aadhaarNumber?.Trim() ?? string.Empty;
        panNumber = panNumber?.Trim().ToUpperInvariant() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(customerName) ||
            string.IsNullOrWhiteSpace(aadhaarNumber) ||
            string.IsNullOrWhiteSpace(panNumber))
        {
            return BadRequest(new
            {
                success = false,
                message = "Customer name, Aadhaar and PAN are required."
            });
        }

        bool aadhaarExists =
            await _customerRepository.IsAadhaarExistsAsync(aadhaarNumber);

        bool panExists =
            await _customerRepository.IsPANExistsAsync(panNumber);

        bool sameCustomer =
            await _customerRepository.IsSameCustomerAsync(
                aadhaarNumber, panNumber, customerName);

        if ((aadhaarExists || panExists) && !sameCustomer)
        {
            var fieldErrors = new Dictionary<string, string>();

            if (aadhaarExists)
            {
                fieldErrors[nameof(aadhaarNumber)] =
                    "This Aadhaar number is already registered with another customer.";
            }

            if (panExists)
            {
                fieldErrors[nameof(panNumber)] =
                    "This PAN number is already registered with another customer.";
            }

            return Ok(new
            {
                success = false,
                message = "Applicant verification failed. Check the highlighted identity details.",
                fieldErrors
            });
        }

        if (sameCustomer)
        {
            int activeLoans =
                await _customerRepository.GetActiveLoanCountAsync(
                    aadhaarNumber, panNumber, customerName);

            if (activeLoans >= 3)
            {
                return Ok(new
                {
                    success = false,
                    message = "This customer already has the maximum of 3 loans. Closed or fully paid loans are included."
                });
            }

            return Ok(new
            {
                success = true,
                message = $"Customer verified. Existing active loans: {activeLoans}. You can register another loan."
            });
        }

        return Ok(new
        {
            success = true,
            message = "New customer verified. You can proceed with registration."
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateDocuments(
        IFormFile? aadhaarDocument,
        IFormFile? panDocument,
        string aadhaarNumber,
        string panNumber,
        CancellationToken cancellationToken)
    {
        aadhaarNumber = aadhaarNumber?.Trim() ?? string.Empty;
        panNumber = panNumber?.Trim().ToUpperInvariant() ?? string.Empty;

        var fieldErrors = new Dictionary<string, string>();

        if (!await IsValidDocumentAsync(aadhaarDocument))
        {
            fieldErrors[nameof(aadhaarDocument)] =
                "Upload a valid Aadhaar image or PDF document.";
        }

        if (!await IsValidDocumentAsync(panDocument))
        {
            fieldErrors[nameof(panDocument)] =
                "Upload a valid PAN image or PDF document.";
        }

        if (fieldErrors.Count == 0)
        {
            DocumentValidationResult aadhaarValidation =
                await _documentValidationService.ValidateAadhaarAsync(
                    aadhaarDocument!, aadhaarNumber, cancellationToken);

            DocumentValidationResult panValidation =
                await _documentValidationService.ValidatePanAsync(
                    panDocument!, panNumber, cancellationToken);

            if (!aadhaarValidation.IsValid)
            {
                fieldErrors[nameof(aadhaarDocument)] = aadhaarValidation.Message;
            }

            if (!panValidation.IsValid)
            {
                fieldErrors[nameof(panDocument)] = panValidation.Message;
            }

            if (fieldErrors.Count == 0 &&
                !string.IsNullOrWhiteSpace(aadhaarValidation.Message))
            {
                return Ok(new
                {
                    success = true,
                    message = aadhaarValidation.Message
                });
            }
        }

        return Ok(new
        {
            success = fieldErrors.Count == 0,
            message = fieldErrors.Count == 0
                ? "Identity documents verified."
                : "Document verification failed. Check the highlighted uploads.",
            fieldErrors
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        CustomerRegistrationViewModel model)
    {
        ViewBag.IsCustomerVerified = true;

        model.CustomerName = model.CustomerName?.Trim() ?? string.Empty;
        model.AadhaarNumber = model.AadhaarNumber?.Trim() ?? string.Empty;
        model.PANNumber = model.PANNumber?.Trim().ToUpperInvariant() ?? string.Empty;

        if (!ModelState.IsValid)
            return View(model);

        // Recheck customer details before registration.
        bool aadhaarExists =
            await _customerRepository.IsAadhaarExistsAsync(
                model.AadhaarNumber);

        bool panExists =
            await _customerRepository.IsPANExistsAsync(
                model.PANNumber);

        bool sameCustomer =
            await _customerRepository.IsSameCustomerAsync(
                model.AadhaarNumber,
                model.PANNumber,
                model.CustomerName);

        if ((aadhaarExists || panExists) && !sameCustomer)
        {
            if (aadhaarExists)
            {
                ModelState.AddModelError(nameof(model.AadhaarNumber),
                    "This Aadhaar number is already registered with another customer.");
            }

            if (panExists)
            {
                ModelState.AddModelError(nameof(model.PANNumber),
                    "This PAN number is already registered with another customer.");
            }

            return View(model);
        }

        if (sameCustomer)
        {
            int activeLoans =
                await _customerRepository.GetActiveLoanCountAsync(
                    model.AadhaarNumber,
                    model.PANNumber,
                    model.CustomerName);

            if (activeLoans >= 3)
            {
                ModelState.AddModelError(string.Empty,
                    "This customer already has the maximum of 3 loans. Closed or fully paid loans are included.");

                return View(model);
            }
        }

        // Calculate EMI from existing configuration.
        var emiAmount = _emiService.GetEMIAmount(model.LoanAmount);

        if (emiAmount == null)
        {
            ModelState.AddModelError(nameof(model.LoanAmount),
                "EMI configuration not available for this loan amount.");

            return View(model);
        }

        if (!await IsValidImageAsync(model.Photo))
        {
            ModelState.AddModelError(nameof(model.Photo),
                "Upload a valid JPG, JPEG, PNG, or WEBP customer photo up to 5 MB.");
        }

        if (!await IsValidImageAsync(model.SignatureImage))
        {
            ModelState.AddModelError(nameof(model.SignatureImage),
                "Upload a valid JPG, JPEG, PNG, or WEBP signature image up to 5 MB.");
        }

        if (!await IsValidDocumentAsync(model.AadhaarDocument))
        {
            ModelState.AddModelError(nameof(model.AadhaarDocument),
                "Upload a valid Aadhaar JPG, JPEG, PNG, WEBP, or PDF file up to 10 MB.");
        }

        if (!await IsValidDocumentAsync(model.PANDocument))
        {
            ModelState.AddModelError(nameof(model.PANDocument),
                "Upload a valid PAN JPG, JPEG, PNG, WEBP, or PDF file up to 10 MB.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        DocumentValidationResult aadhaarValidation =
            await _documentValidationService.ValidateAadhaarAsync(
                model.AadhaarDocument!, model.AadhaarNumber);

        DocumentValidationResult panValidation =
            await _documentValidationService.ValidatePanAsync(
                model.PANDocument!, model.PANNumber);

        if (!aadhaarValidation.IsValid)
        {
            ModelState.AddModelError(nameof(model.AadhaarDocument),
                aadhaarValidation.Message);
        }

        if (!panValidation.IsValid)
        {
            ModelState.AddModelError(nameof(model.PANDocument),
                panValidation.Message);
        }

        if (!ModelState.IsValid)
            return View(model);

        string? photoPath = null;
        string? signaturePath = null;
        string? aadhaarDocumentPath = null;
        string? panDocumentPath = null;

        try
        {
            photoPath = await SaveImageAsync(model.Photo!);

            signaturePath = await SaveImageAsync(
                model.SignatureImage!);

            aadhaarDocumentPath = await SaveDocumentAsync(
                model.AadhaarDocument!);

            panDocumentPath = await SaveDocumentAsync(
                model.PANDocument!);

            var customer = new Customer
            {
                CustomerName = model.CustomerName,
                AadhaarNumber = model.AadhaarNumber,
                PANNumber = model.PANNumber,

                PhotoPath = photoPath,
                SignatureImagePath = signaturePath,
                AadhaarDocumentPath = aadhaarDocumentPath,
                PANDocumentPath = panDocumentPath,

                ReferredPerson = model.ReferredPerson,
                ReferredContactNo = model.ReferredContactNo,
                PrimaryContactNo = model.PrimaryContactNo,
                SecondaryContactNo = model.SecondaryContactNo,

                LoanAmount = model.LoanAmount,
                EMIAmount = emiAmount.Value,
                InstallmentCount = _emiService.GetInstallmentCount(),

                IsLive = true,
                CreatedBy = User.Identity?.Name ?? "Admin",
                Remarks = model.Remarks
            };

            string loanAccountNo =
                await _customerRepository.RegisterCustomerAsync(customer);

            TempData["SuccessMessage"] =
                $"Customer registered successfully. Loan Account Number: {loanAccountNo}";

            return RedirectToAction(nameof(Register));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Customer registration failed.");

            DeleteUploadedFile(photoPath);
            DeleteUploadedFile(signaturePath);
            DeleteUploadedFile(aadhaarDocumentPath);
            DeleteUploadedFile(panDocumentPath);

            ModelState.AddModelError(string.Empty,
                "Customer registration failed. Please try again.");

            return View(model);
        }
    }

    private static async Task<bool> IsValidImageAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0 || file.Length > 5 * 1024 * 1024)
            return false;

        string extension =
            Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension))
            return false;

        return await HasImageSignatureAsync(file, extension);
    }

    private static async Task<bool> IsValidDocumentAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0 || file.Length > 10 * 1024 * 1024)
            return false;

        string extension =
            Path.GetExtension(file.FileName).ToLowerInvariant();

        if (extension == ".pdf")
        {
            byte[] header = await ReadHeaderAsync(file, 5);
            return header.Length == 5 && Encoding.ASCII.GetString(header) == "%PDF-";
        }

        if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(extension))
            return false;

        return await HasImageSignatureAsync(file, extension);
    }

    private static async Task<bool> HasImageSignatureAsync(
        IFormFile file,
        string extension)
    {
        byte[] header = await ReadHeaderAsync(file, 12);

        bool isJpeg = extension is ".jpg" or ".jpeg" &&
                      header.Length >= 3 &&
                      header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

        bool isPng = extension == ".png" &&
                     header.Length >= 8 &&
                     header[0] == 0x89 && header[1] == 0x50 &&
                     header[2] == 0x4E && header[3] == 0x47 &&
                     header[4] == 0x0D && header[5] == 0x0A &&
                     header[6] == 0x1A && header[7] == 0x0A;

        bool isWebp = extension == ".webp" &&
                      header.Length >= 12 &&
                      Encoding.ASCII.GetString(header, 0, 4) == "RIFF" &&
                      Encoding.ASCII.GetString(header, 8, 4) == "WEBP";

        return isJpeg || isPng || isWebp;
    }

    private static async Task<byte[]> ReadHeaderAsync(IFormFile file, int length)
    {
        await using Stream stream = file.OpenReadStream();
        byte[] header = new byte[length];
        int read = await stream.ReadAsync(header.AsMemory(0, length));

        return read == length ? header : header[..read];
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        string folder = Path.Combine(
            _environment.WebRootPath,
            "uploads",
            "customers");

        Directory.CreateDirectory(folder);

        string extension =
            Path.GetExtension(file.FileName).ToLowerInvariant();

        string fileName = $"{Guid.NewGuid():N}{extension}";

        string fullPath = Path.Combine(folder, fileName);

        await using var stream = new FileStream(
            fullPath, FileMode.CreateNew);

        await file.CopyToAsync(stream);

        return $"/uploads/customers/{fileName}";
    }

    private async Task<string> SaveDocumentAsync(IFormFile file)
    {
        return await SaveImageAsync(file);
    }

    private void DeleteUploadedFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        string fileName = Path.GetFileName(relativePath);

        string fullPath = Path.Combine(
            _environment.WebRootPath,
            "uploads",
            "customers",
            fileName);

        if (System.IO.File.Exists(fullPath))
            System.IO.File.Delete(fullPath);
    }
}
