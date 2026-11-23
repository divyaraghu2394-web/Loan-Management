
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
            model.CollectedInstallments = model.Collections.Count();
            model.RemainingInstallments = Math.Max(
                0,
                model.Customer.InstallmentCount - model.CollectedInstallments);
            model.IsLoanCompleted = model.RemainingInstallments == 0;

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
            model.CollectedInstallments = model.Collections.Count();
            model.RemainingInstallments = Math.Max(
                0,
                model.Customer.InstallmentCount - model.CollectedInstallments);
            model.IsLoanCompleted = model.RemainingInstallments == 0;
            model.NextInstallmentNumber = model.IsLoanCompleted
                ? 0
                : GetNextMissingInstallmentNumber(
                    model.Collections,
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

                model.CollectedInstallments =
                    model.Collections.Count();

                model.RemainingInstallments =
                    Math.Max(
                        0,
                        model.Customer.InstallmentCount -
                        model.CollectedInstallments);

                model.IsLoanCompleted = model.RemainingInstallments == 0;
                model.NextInstallmentNumber = model.IsLoanCompleted
                    ? 0
                    : GetNextMissingInstallmentNumber(
                        model.Collections,
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

                int remaining = Math.Max(
                    0,
                    customer.InstallmentCount - collections.Count);

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

                bool penaltyApplicable = IsPenaltyApplicable(collectionDate);
                decimal suggestedPenalty = _emiService
                    .GetSuggestedPenalty(customer.LoanAmount);
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
                        CreatedOn = DateTime.UtcNow
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
        public async Task<IActionResult> Delete(long collectionId, string loanAccountNo)
        {
            var customer = await _emiCollectionRepository
                .GetCustomerLoanDetailsAsync(loanAccountNo?.Trim() ?? string.Empty);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Active loan not found.";
                return RedirectToAction(nameof(Index));
            }

            var collections = await _emiCollectionRepository
                .GetCollectionsByLoanAccountNoAsync(customer.LoanAccountNo);

            if (collections.Count() >= customer.InstallmentCount)
            {
                TempData["ErrorMessage"] =
                    "This loan is fully paid. Completed EMI records cannot be deleted from this screen.";

                return RedirectToAction(nameof(SearchLoan), new
                {
                    LoanAccountNo = customer.LoanAccountNo
                });
            }

            bool deleted = await _emiCollectionRepository
                .DeleteCollectionAsync(collectionId);

            TempData[deleted ? "SuccessMessage" : "ErrorMessage"] = deleted
                ? "Collection record deleted successfully."
                : "Collection record was not found.";

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
    }
}
