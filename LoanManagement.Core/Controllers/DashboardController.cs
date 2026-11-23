
using LoanManagement.Core.ViewModels;
using LoanManagement.DL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanManagement.Core.Controllers;

[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IEMICollectionRepository _emiCollectionRepository;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IEMICollectionRepository emiCollectionRepository,
        ILogger<DashboardController> logger)
    {
        _emiCollectionRepository = emiCollectionRepository;
        _logger = logger;
    }

        public async Task<IActionResult> Index(string? searchTerm)
    {
        try
        {
            var today = DateTime.Today;
                searchTerm = searchTerm?.Trim() ?? string.Empty;

            return View(new DashboardViewModel
            {
                BusinessDate = today,
                    SearchTerm = searchTerm,
                DailyCollections = await _emiCollectionRepository
                    .GetDailyCollectionSummaryAsync(today),
                DailyCollectionRecords = await _emiCollectionRepository
                    .GetDailyCollectionRecordsAsync(today),
                ActiveLoans = await _emiCollectionRepository
                        .GetActiveLoansAsync(searchTerm)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to load the admin dashboard.");

            TempData["ErrorMessage"] =
                "Some dashboard data could not be loaded.";

            return View(new DashboardViewModel());
        }
    }
}
