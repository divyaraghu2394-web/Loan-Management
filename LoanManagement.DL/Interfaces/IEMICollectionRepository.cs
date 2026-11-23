
using LoanManagement.DL.Models;

namespace LoanManagement.DL.Interfaces
{
    public interface IEMICollectionRepository
    {
        // Get all active customer loans for EMI grid
        Task<IEnumerable<CustomerLoanDetails>> GetActiveLoansAsync(
            string? customerName = null);

        Task<DailyCollectionSummary> GetDailyCollectionSummaryAsync(
            DateTime collectionDate);

        Task<IEnumerable<DailyCollectionRecord>> GetDailyCollectionRecordsAsync(
            DateTime collectionDate);

        // Get collection history for a loan
        Task<IEnumerable<EMICollection>> GetCollectionsByLoanAccountNoAsync(
            string loanAccountNo);

        // Get individual customer loan details
        Task<CustomerLoanDetails?> GetCustomerLoanDetailsAsync(
            string loanAccountNo);

        // Save EMI collection
        Task<bool> CollectEMIAsync(EMICollection collection);

        Task<bool> CollectEMIsAsync(
            EMICollection collection,
            int installmentCount);

        Task<bool> DeleteCollectionAsync(long collectionId);
    }
}
