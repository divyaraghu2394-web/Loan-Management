
using LoanManagement.Core.ViewModels;
using LoanManagement.Core.Services;
using LoanManagement.DL.Models;
using LoanManagement.DL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanManagement.Core.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EMICollectionController : Controller
    {
        private readonly IEMICollectionRepository _emiCollectionRepository;
        private readonly ILogger<EMICollectionController> _logger;
        private readonly EMICalculationService _emiService;

        public EMICollectionController(
            IEMICollectionRepository emiCollectionRepository,
            ILogger<EMICollectionController> logger,
            EMICalculationService emiService)
        {
            _emiCollectionRepository = emiCollectionRepository;
            _logger = logger;
            _emiService = emiService;
        }

        // GET: EMICollection
        // Display all active customers in grid
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var model = new EMICollectionViewModel
                {
                    CollectionDate = DateTime.Today,

                    ActiveLoans = await _emiCollectionRepository
                        .GetActiveLoansAsync()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading EMI customer grid");

                TempData["ErrorMessage"] =
                    "Unable to load customer loan details.";

                return View(new EMICollectionViewModel
                {
                    CollectionDate = DateTime.Today
                });
            }

        }

        [HttpGet]
        public IActionResult Report()
        {
            return View(new EMICollectionViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Report(EMICollectionViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.LoanAccountNo))
            {
                ModelState.AddModelError(
                    nameof(model.LoanAccountNo),
                    "Enter a loan account number.");

                return View(model);
            }

            model.LoanAccountNo = model.LoanAccountNo.Trim();
            model.Customer = await _emiCollectionRepository
                .GetCustomerLoanDetailsAsync(model.LoanAccountNo);

            if (model.Customer == null)
            {
                ModelState.AddModelError(
                    nameof(model.LoanAccountNo),
                    "Loan account not found.");

                return View(model);
            }

            model.Collections = (await _emiCollectionRepository
                .GetCollectionsByLoanAccountNoAsync(model.LoanAccountNo))
                .ToList();
            ApplyCollectionSummary(model);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SearchLoan(string loanAccountNo)
        {
            if (string.IsNullOrWhiteSpace(loanAccountNo))
                return RedirectToAction(nameof(Index));

            var model = new EMICollectionViewModel
            {
                LoanAccountNo = loanAccountNo.Trim(),
                CollectionDate = DateTime.Today
            };

            model.Customer = await _emiCollectionRepository
                .GetCustomerLoanDetailsAsync(model.LoanAccountNo);

            if (model.Customer == null)
            {
                TempData["ErrorMessage"] = "Active loan not found.";
                return RedirectToAction(nameof(Index));
            }

            model.Collections = (await _emiCollectionRepository
                .GetCollectionsByLoanAccountNoAsync(model.LoanAccountNo))
                .ToList();
            ApplyCollectionSummary(model);
            model.NextInstallmentNumber = model.IsLoanCompleted
                ? 0
                : GetNextMissingInstallmentNumber(
                    model.Collections.Where(x => !x.IsReversalEntry && !x.IsReversed),
                    model.Customer.InstallmentCount);
            model.InstallmentsToCollect = 1;
            model.SuggestedPenaltyAmount = _emiService
                .GetSuggestedPenalty(model.Customer.LoanAmount);
            model.IsPenaltyApplicable = IsPenaltyApplicable(model.CollectionDate);
            model.PenaltyAmount = null;

            return View("Index", model);
        }

        // POST: EMICollection/SearchLoan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SearchLoan(
            EMICollectionViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.LoanAccountNo))
            {
                ModelState.AddModelError(
                    nameof(model.LoanAccountNo),
                    "Please enter Loan Account Number.");

                return View("Index", model);
            }

            try
            {
                model.LoanAccountNo = model.LoanAccountNo.Trim();

                model.Customer =
                    await _emiCollectionRepository
                        .GetCustomerLoanDetailsAsync(model.LoanAccountNo);

                if (model.Customer == null)
                {
                    ModelState.AddModelError(
                        nameof(model.LoanAccountNo),
                        "Active loan not found.");

                    return View("Index", model);
                }

                var collections =
                    await _emiCollectionRepository
                        .GetCollectionsByLoanAccountNoAsync(
                            model.LoanAccountNo);

                model.Collections = collections.ToList();
                ApplyCollectionSummary(model);
                model.NextInstallmentNumber = model.IsLoanCompleted
                    ? 0
                    : GetNextMissingInstallmentNumber(
                        model.Collections.Where(x => !x.IsReversalEntry && !x.IsReversed),
                        model.Customer.InstallmentCount);

                model.InstallmentsToCollect = 1;
                model.SuggestedPenaltyAmount = _emiService
                    .GetSuggestedPenalty(model.Customer.LoanAmount);
                model.IsPenaltyApplicable = IsPenaltyApplicable(model.CollectionDate);
                model.CollectionDate = DateTime.Today;
                model.PenaltyAmount = null;

                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error searching loan account {LoanAccountNo}",
                    model.LoanAccountNo);

                ModelState.AddModelError(
                    string.Empty,
                    "Unable to retrieve loan details. Please try again.");

                return View("Index", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Collect(EMICollectionViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.LoanAccountNo))
                return RedirectToAction(nameof(Index));

            try
            {
                var customer = await _emiCollectionRepository
                    .GetCustomerLoanDetailsAsync(model.LoanAccountNo.Trim());

                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Active loan not found.";
                    return RedirectToAction(nameof(Index));
                }

                var collections = (await _emiCollectionRepository
                    .GetCollectionsByLoanAccountNoAsync(model.LoanAccountNo))
                    .ToList();

                var activeCollections = collections
                    .Where(x => !x.IsReversalEntry && !x.IsReversed)
                    .ToList();

                int remaining = Math.Max(
                    0,
                    customer.InstallmentCount - activeCollections.Count);

                if (remaining == 0)
                {
                    TempData["ErrorMessage"] =
                        "This loan is fully paid. No further EMI can be collected.";

                    return RedirectToAction(nameof(SearchLoan), new
                    {
                        LoanAccountNo = customer.LoanAccountNo
                    });
                }

                if (model.InstallmentsToCollect < 1 ||
                    model.InstallmentsToCollect > remaining)
                {
                    TempData["ErrorMessage"] =
                        $"Select between 1 and {remaining} remaining installments.";
                    return RedirectToAction(nameof(SearchLoan), new
                    {
                        LoanAccountNo = model.LoanAccountNo
                    });
                }

                DateTime collectionDate = model.CollectionDate == default
                    ? DateTime.Today
                    : model.CollectionDate.Date;

                if (collectionDate > DateTime.Today)
                {
                    TempData["ErrorMessage"] =
                        "Collection date cannot be in the future.";
                    return RedirectToAction(nameof(SearchLoan), new
                    {
                        LoanAccountNo = customer.LoanAccountNo
                    });
                }

                if (model.PenaltyAmount.HasValue && model.PenaltyAmount.Value < 0)
                {
                    TempData["ErrorMessage"] =
                        "Penalty amount cannot be negative.";
                    return RedirectToAction(nameof(SearchLoan), new
                    {
                        LoanAccountNo = customer.LoanAccountNo
                    });
                }

                string[] paymentModes =
                    ["Cash", "UPI", "Bank Transfer", "Cheque", "Other"];

                if (!paymentModes.Contains(model.PaymentMode, StringComparer.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "Select a valid payment mode.";
                    return RedirectToAction(nameof(SearchLoan), new
                    {
                        LoanAccountNo = customer.LoanAccountNo
                    });
                }

                if (!string.Equals(model.PaymentMode, "Cash", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrWhiteSpace(model.PaymentReference))
                {
                    TempData["ErrorMessage"] =
                        "Payment reference is required for non-cash payments.";
                    return RedirectToAction(nameof(SearchLoan), new
                    {
                        LoanAccountNo = customer.LoanAccountNo
                    });
                }

                decimal penalty = model.PenaltyAmount.GetValueOrDefault() > 0
                    ? model.PenaltyAmount.GetValueOrDefault()
                    : 0;

                bool saved = await _emiCollectionRepository.CollectEMIsAsync(
                    new EMICollection
                    {
                        LoanAccountNo = customer.LoanAccountNo,
                        EMIAmount = customer.EMIAmount,
                        PenaltyAmount = penalty,
                        CollectionDate = collectionDate,
                        CreatedBy = User.Identity?.Name ?? "Admin",
                        CreatedOn = DateTime.UtcNow,
                        PaymentMode = model.PaymentMode,
                        PaymentReference = model.PaymentReference?.Trim(),
                        Remarks = model.Remarks?.Trim()
                    },
                    model.InstallmentsToCollect);

                TempData[saved ? "SuccessMessage" : "ErrorMessage"] = saved
                    ? $"{model.InstallmentsToCollect} installment(s) collected successfully."
                    : "Unable to save the EMI collection.";

                return RedirectToAction(nameof(SearchLoan), new
                {
                    LoanAccountNo = customer.LoanAccountNo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting EMI for {LoanAccountNo}", model.LoanAccountNo);
                TempData["ErrorMessage"] = "Unable to save the EMI collection.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reverse(
            long collectionId,
            string loanAccountNo,
            string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "A reversal reason is required.";
                return RedirectToAction(nameof(SearchLoan), new
                {
                    LoanAccountNo = loanAccountNo
                });
            }

            var customer = await _emiCollectionRepository
                .GetCustomerLoanDetailsAsync(loanAccountNo?.Trim() ?? string.Empty);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Active loan not found.";
                return RedirectToAction(nameof(Index));
            }

            var collections = (await _emiCollectionRepository
                .GetCollectionsByLoanAccountNoAsync(customer.LoanAccountNo))
                .ToList();

            int activeCollectionCount = collections.Count(x => !x.IsReversalEntry && !x.IsReversed);
            bool isLoanClosed = !customer.IsLive ||
                activeCollectionCount >= customer.InstallmentCount;

            if (isLoanClosed)
            {
                TempData["ErrorMessage"] =
                    "Collections cannot be reversed for a closed loan.";
                return RedirectToAction(nameof(SearchLoan), new
                {
                    LoanAccountNo = customer.LoanAccountNo
                });
            }

            var targetCollection = collections.FirstOrDefault(x => x.CollectionId == collectionId);
            if (targetCollection == null || targetCollection.IsReversalEntry || targetCollection.IsReversed)
            {
                TempData["ErrorMessage"] =
                    "Only an active collection entry can be reversed.";
                return RedirectToAction(nameof(SearchLoan), new
                {
                    LoanAccountNo = customer.LoanAccountNo
                });
            }

            bool reversed = await _emiCollectionRepository.ReverseCollectionAsync(
                collectionId,
                reason.Trim(),
                User.Identity?.Name ?? "Admin");

            TempData[reversed ? "SuccessMessage" : "ErrorMessage"] = reversed
                ? "Collection reversed successfully. A contra reversal entry has been posted to the ledger."
                : "Collection record was not found or was already reversed.";

            return RedirectToAction(nameof(SearchLoan), new
            {
                LoanAccountNo = loanAccountNo
            });
        }

        private static bool IsPenaltyApplicable(DateTime collectionDate)
        {
            int daysSinceWednesday =
                (7 + (int)collectionDate.DayOfWeek - (int)DayOfWeek.Wednesday) % 7;

            DateTime dueDate = collectionDate.Date.AddDays(-daysSinceWednesday);
            return collectionDate.Date > dueDate.AddDays(2);
        }

        private static int GetNextMissingInstallmentNumber(
            IEnumerable<EMICollection> collections,
            int installmentCount)
        {
            var collectedInstallments = collections
                .Select(x => x.InstallmentNumber)
                .ToHashSet();

            for (int installmentNumber = 1;
                 installmentNumber <= installmentCount;
                 installmentNumber++)
            {
                if (!collectedInstallments.Contains(installmentNumber))
                    return installmentNumber;
            }

            return 0;
        }

        private static void ApplyCollectionSummary(EMICollectionViewModel model)
        {
            if (model.Customer == null)
                return;

            var collections = model.Collections.ToList();

            var activeCollections = collections
                .Where(x => !x.IsReversalEntry && !x.IsReversed)
                .ToList();

            model.CollectedInstallments = activeCollections.Count;
            model.RemainingInstallments = Math.Max(
                0,
                model.Customer.InstallmentCount - model.CollectedInstallments);
            model.TotalCollectedAmount = collections.Sum(x => x.TotalAmount ??
                (x.EMIAmount + x.PenaltyAmount));
            model.TotalPenaltyCollected = collections.Sum(x => x.PenaltyAmount);
            model.OutstandingAmount = model.Customer.EMIAmount *
                model.RemainingInstallments;
            model.IsLoanCompleted = model.RemainingInstallments == 0;
            model.CollectionStatus = model.IsLoanCompleted
                ? "Fully Paid"
                : "Active";
        }
    }
}
