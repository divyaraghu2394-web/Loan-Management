
namespace LoanManagement.Core.Models;

public class EMISettings
{
    public int InstallmentCount { get; set; } = 10;

    public List<EMIRate> EMIRates { get; set; } = new();

    public List<PenaltyRule> PenaltyRules { get; set; } = new();

    public int OverdueDays { get; set; } = 3;
}

public class EMIRate
{
    public decimal LoanAmount { get; set; }

    public decimal EMIAmount { get; set; }
}

public class PenaltyRule
{
    public decimal MinimumLoanAmount { get; set; }

    public decimal MaximumLoanAmount { get; set; }

    public decimal PenaltyAmount { get; set; }
}
