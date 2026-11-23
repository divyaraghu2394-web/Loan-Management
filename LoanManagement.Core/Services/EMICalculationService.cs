
using LoanManagement.Core.Models;

namespace LoanManagement.Core.Services;

public class EMICalculationService
{
    private readonly IConfiguration _configuration;

    public EMICalculationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private EMISettings GetSettings()
    {
        return _configuration.Get<EMISettings>()
            ?? throw new InvalidOperationException(
                "EMISettings configuration is missing.");
    }

    // Get EMI based on exact loan amount
    public decimal? GetEMIAmount(decimal loanAmount)
    {
        var settings = GetSettings();

        var rate = settings.EMIRates.FirstOrDefault(
            x => x.LoanAmount == loanAmount);

        return rate?.EMIAmount;
    }

    // Get suggested penalty based on loan amount
    public decimal GetSuggestedPenalty(decimal loanAmount)
    {
        var settings = GetSettings();

        var rule = settings.PenaltyRules.FirstOrDefault(
            x => loanAmount >= x.MinimumLoanAmount &&
                 loanAmount <= x.MaximumLoanAmount);

        return rule?.PenaltyAmount ?? 0;
    }

    // Wednesday due date:
    // Wednesday, Thursday and Friday are penalty-free.
    // Penalty starts on Saturday.
    public bool IsPenaltyApplicable(
        DateTime dueDate,
        DateTime collectionDate)
    {
        var settings = GetSettings();

        var lastPenaltyFreeDate =
            dueDate.Date.AddDays(settings.OverdueDays - 1);

        return collectionDate.Date > lastPenaltyFreeDate;
    }

    public int GetInstallmentCount()
    {
        return GetSettings().InstallmentCount;
    }
}
