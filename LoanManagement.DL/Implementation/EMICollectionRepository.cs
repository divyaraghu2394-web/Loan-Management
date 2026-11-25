
using Dapper;
using LoanManagement.DL.Interfaces;
using LoanManagement.DL.Models;

namespace LoanManagement.DL.Implementation
{
    public class EMICollectionRepository : IEMICollectionRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public EMICollectionRepository(
            IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        // Get all active customer loans for EMI grid
        public async Task<IEnumerable<CustomerLoanDetails>> GetActiveLoansAsync(
            string? customerName = null)
        {
            const string sql = @"
                SELECT
                    C.LoanAccountNo,
                    C.CustomerName,
                    C.LoanAmount,
                    C.EMIAmount,
                    C.InstallmentCount,
                    C.IsLive,
                    C.ReferredPerson,
                    C.PrimaryContactNo,
                    C.PhotoPath,
                    C.CreatedOn AS LoanCreatedOn,
                    MAX(E.CollectionDate) AS LastCollectionDate,

                    COUNT(E.CollectionId) AS CollectedInstallments,

                    CASE
                        WHEN C.InstallmentCount - COUNT(E.CollectionId) < 0
                        THEN 0
                        ELSE C.InstallmentCount - COUNT(E.CollectionId)
                    END AS RemainingInstallments,

                    C.EMIAmount *
                    CASE
                        WHEN C.InstallmentCount - COUNT(E.CollectionId) < 0
                        THEN 0
                        ELSE C.InstallmentCount - COUNT(E.CollectionId)
                    END AS PendingAmount

                FROM Customers C

                LEFT JOIN EMICollections E
                    ON C.LoanAccountNo = E.LoanAccountNo
                   AND E.IsReversed = 0
                   AND E.EntryType = 'Collection'

                WHERE C.IsLive = 1
                  AND (@CustomerName IS NULL
                       OR @CustomerName = ''
                       OR C.CustomerName LIKE '%' + @CustomerName + '%')

               GROUP BY
    C.LoanAccountNo,
    C.CustomerName,
    C.LoanAmount,
    C.EMIAmount,
    C.InstallmentCount,
    C.IsLive,
    C.ReferredPerson,
    C.PrimaryContactNo,
    C.PhotoPath,
    C.CreatedOn

                ORDER BY C.CreatedOn DESC;";

            using var connection = _connectionFactory.CreateConnection();

            var loans = (await connection.QueryAsync<CustomerLoanDetails>(
                sql,
                new { CustomerName = customerName?.Trim() })).ToList();

            foreach (var loan in loans)
            {
                loan.UpcomingEMIDates = CalculateUpcomingEMIDates(loan);
                loan.NextEMIDate = loan.UpcomingEMIDates.FirstOrDefault();
            }

            return loans
                .OrderBy(loan => loan.NextEMIDate ?? DateTime.MaxValue)
                .ThenByDescending(loan => loan.LoanCreatedOn);
        }

        private static List<DateTime> CalculateUpcomingEMIDates(
            CustomerLoanDetails loan)
        {
            if (loan.RemainingInstallments <= 0)
            {
                return new List<DateTime>();
            }

            DateTime referenceDate = loan.LastCollectionDate.HasValue
                ? loan.LastCollectionDate.Value.Date.AddDays(1)
                : loan.LoanCreatedOn.Date;

            int daysUntilWednesday =
                ((int)DayOfWeek.Wednesday - (int)referenceDate.DayOfWeek + 7) % 7;

            DateTime nextEMIDate = referenceDate.AddDays(daysUntilWednesday);

            return Enumerable.Range(0, loan.RemainingInstallments)
                .Select(installment => nextEMIDate.AddDays(installment * 7))
                .ToList();
        }

        public async Task<DailyCollectionSummary> GetDailyCollectionSummaryAsync(
            DateTime collectionDate)
        {
            const string sql = @"
                SELECT
                    COUNT(CASE WHEN EntryType = 'Collection' AND IsReversed = 0 THEN 1 END) AS CollectionCount,
                    COALESCE(SUM(EMIAmount), 0) AS EMIAmount,
                    COALESCE(SUM(PenaltyAmount), 0) AS PenaltyAmount,
                    COALESCE(SUM(EMIAmount + PenaltyAmount), 0) AS TotalAmount
                FROM EMICollections
                WHERE CollectionDate >= @CollectionDate
                  AND CollectionDate < DATEADD(day, 1, @CollectionDate);";

            using var connection = _connectionFactory.CreateConnection();

            return await connection.QuerySingleAsync<DailyCollectionSummary>(
                sql,
                new { CollectionDate = collectionDate.Date });
        }

        public async Task<IEnumerable<DailyCollectionRecord>>
            GetDailyCollectionRecordsAsync(DateTime collectionDate)
        {
            const string sql = @"
                SELECT
                    E.LoanAccountNo,
                    C.CustomerName,
                    E.EMIAmount,
                    E.PenaltyAmount,
                    E.TotalAmount,
                    E.CollectionDate,
                    E.InstallmentNumber
                FROM EMICollections E
                INNER JOIN Customers C
                    ON C.LoanAccountNo = E.LoanAccountNo
                WHERE E.CollectionDate >= @CollectionDate
                  AND E.CollectionDate < DATEADD(day, 1, @CollectionDate)
                ORDER BY E.CollectionDate DESC, E.CollectionId DESC;";

            using var connection = _connectionFactory.CreateConnection();

            return await connection.QueryAsync<DailyCollectionRecord>(
                sql,
                new { CollectionDate = collectionDate.Date });
        }

        // Get collection history for a loan
        public async Task<IEnumerable<EMICollection>>
            GetCollectionsByLoanAccountNoAsync(string loanAccountNo)
        {
            const string sql = @"
                SELECT
                    CollectionId,
                    LoanAccountNo,
                    EMIAmount,
                    PenaltyAmount,
                    TotalAmount,
                    CollectionDate,
                    CreatedBy,
                    CreatedOn,
                    InstallmentNumber,
                    ReceiptNumber,
                    PaymentMode,
                    PaymentReference,
                    Remarks,
                    IsReversed,
                    ReversalReason,
                    ReversedBy,
                    ReversedOn,
                    EntryType,
                    ParentCollectionId
                FROM EMICollections
                WHERE LoanAccountNo = @LoanAccountNo
                ORDER BY CollectionDate, CollectionId;";

            using var connection = _connectionFactory.CreateConnection();

            return await connection.QueryAsync<EMICollection>(
                sql,
                new { LoanAccountNo = loanAccountNo });
        }

        // Get individual customer loan details
        public async Task<CustomerLoanDetails?>
            GetCustomerLoanDetailsAsync(string loanAccountNo)
        {
            const string sql = @"
                SELECT
                    C.LoanAccountNo,
                    C.CustomerName,
                    C.LoanAmount,
                    C.EMIAmount,
                    C.InstallmentCount,
                    C.IsLive,
                    C.ReferredPerson,
                    C.PrimaryContactNo,
                    C.PhotoPath,

                    COUNT(E.CollectionId) AS CollectedInstallments,

                    CASE
                        WHEN C.InstallmentCount - COUNT(E.CollectionId) < 0
                        THEN 0
                        ELSE C.InstallmentCount - COUNT(E.CollectionId)
                    END AS RemainingInstallments,

                    C.EMIAmount *
                    CASE
                        WHEN C.InstallmentCount - COUNT(E.CollectionId) < 0
                        THEN 0
                        ELSE C.InstallmentCount - COUNT(E.CollectionId)
                    END AS PendingAmount

                FROM Customers C

                LEFT JOIN EMICollections E
                    ON C.LoanAccountNo = E.LoanAccountNo
                   AND E.IsReversed = 0
                   AND E.EntryType = 'Collection'

                WHERE C.LoanAccountNo = @LoanAccountNo
                  AND C.IsLive = 1

                GROUP BY
                    C.LoanAccountNo,
                    C.CustomerName,
                    C.LoanAmount,
                    C.EMIAmount,
                    C.InstallmentCount,
                    C.IsLive,
                    C.ReferredPerson,
                    C.PrimaryContactNo,
                    C.PhotoPath;";

            using var connection = _connectionFactory.CreateConnection();

            return await connection.QueryFirstOrDefaultAsync<CustomerLoanDetails>(
                sql,
                new { LoanAccountNo = loanAccountNo });
        }

        // Save EMI collection
        public async Task<bool> CollectEMIAsync(EMICollection collection)
        {
            return await CollectEMIsAsync(collection, 1);
        }

        public async Task<bool> CollectEMIsAsync(
            EMICollection collection,
            int installmentCount)
        {
            if (installmentCount < 1)
                return false;

            const string collectedInstallmentsSql = @"
                SELECT InstallmentNumber
                FROM EMICollections WITH (UPDLOCK, HOLDLOCK)
                WHERE LoanAccountNo = @LoanAccountNo
                  AND IsReversed = 0
                  AND EntryType = 'Collection';";

            const string remainingSql = @"
                SELECT C.InstallmentCount - COUNT(E.CollectionId)
                FROM Customers C
                LEFT JOIN EMICollections E
                    ON C.LoanAccountNo = E.LoanAccountNo
                   AND E.IsReversed = 0
                   AND E.EntryType = 'Collection'
                WHERE C.LoanAccountNo = @LoanAccountNo
                GROUP BY C.InstallmentCount;";

            const string insertSql = @"
                INSERT INTO EMICollections
                (
                    LoanAccountNo,
                    EMIAmount,
                    PenaltyAmount,
                    CollectionDate,
                    CreatedBy,
                    CreatedOn,
                    InstallmentNumber,
                    ReceiptNumber,
                    PaymentMode,
                    PaymentReference,
                    Remarks,
                    EntryType
                )
                VALUES
                (
                    @LoanAccountNo,
                    @EMIAmount,
                    @PenaltyAmount,
                    @CollectionDate,
                    @CreatedBy,
                    @CreatedOn,
                    @InstallmentNumber,
                    @ReceiptNumber,
                    @PaymentMode,
                    @PaymentReference,
                    @Remarks,
                    'Collection'
                );";

            using var connection = _connectionFactory.CreateConnection();
            await ((System.Data.Common.DbConnection)connection).OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                int remaining = await connection.ExecuteScalarAsync<int>(
                    remainingSql,
                    new { collection.LoanAccountNo },
                    transaction);

                if (installmentCount > remaining)
                    return false;

                var collectedInstallments = (await connection.QueryAsync<int>(
                    collectedInstallmentsSql,
                    new { collection.LoanAccountNo },
                    transaction))
                    .ToHashSet();

                var nextInstallments = Enumerable.Range(1, collectedInstallments.Count + remaining)
                    .Where(number => !collectedInstallments.Contains(number))
                    .Take(installmentCount)
                    .ToList();

                if (nextInstallments.Count != installmentCount)
                    return false;

                int rowsAffected = 0;

                const string receiptSql = @"
                    SELECT 'RCT-' + CONVERT(VARCHAR(8), GETDATE(), 112) + '-' +
                        RIGHT('000000' + CONVERT(VARCHAR(20), NEXT VALUE FOR dbo.Seq_EMIReceiptNo), 6);";

                string receiptNumber = await connection.ExecuteScalarAsync<string>(
                    receiptSql,
                    transaction: transaction);

                for (int offset = 0; offset < installmentCount; offset++)
                {
                    var item = new
                    {
                        collection.LoanAccountNo,
                        EMIAmount = collection.EMIAmount,
                        PenaltyAmount = collection.PenaltyAmount,
                        collection.CollectionDate,
                        collection.CreatedBy,
                        collection.CreatedOn,
                        InstallmentNumber = nextInstallments[offset],
                        ReceiptNumber = receiptNumber + "-" + (offset + 1),
                        collection.PaymentMode,
                        collection.PaymentReference,
                        collection.Remarks
                    };

                    rowsAffected += await connection.ExecuteAsync(
                        insertSql,
                        item,
                        transaction);
                }

                transaction.Commit();
                return rowsAffected == installmentCount;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<bool> ReverseCollectionAsync(
            long collectionId,
            string reason,
            string reversedBy)
        {
            const string selectOriginalSql = @"
                SELECT
                    CollectionId,
                    LoanAccountNo,
                    EMIAmount,
                    PenaltyAmount,
                    InstallmentNumber,
                    ReceiptNumber,
                    PaymentMode,
                    PaymentReference
                FROM EMICollections WITH (UPDLOCK, HOLDLOCK)
                WHERE CollectionId = @CollectionId
                  AND EntryType = 'Collection'
                  AND IsReversed = 0;";

            const string loanStatusSql = @"
                SELECT
                    C.IsLive,
                    C.InstallmentCount - COUNT(E.CollectionId) AS RemainingInstallments
                FROM Customers C
                LEFT JOIN EMICollections E
                    ON C.LoanAccountNo = E.LoanAccountNo
                   AND E.IsReversed = 0
                   AND E.EntryType = 'Collection'
                WHERE C.LoanAccountNo = @LoanAccountNo
                GROUP BY C.IsLive, C.InstallmentCount;";

            const string markReversedSql = @"
                UPDATE EMICollections
                SET IsReversed = 1,
                    ReversalReason = @Reason,
                    ReversedBy = @ReversedBy,
                    ReversedOn = SYSDATETIME(),
                    ModifiedBy = @ReversedBy,
                    ModifiedOn = SYSDATETIME()
                WHERE CollectionId = @CollectionId
                  AND EntryType = 'Collection'
                  AND IsReversed = 0;";

            const string reversalReceiptSql = @"
                SELECT 'REV-' + CONVERT(VARCHAR(8), GETDATE(), 112) + '-' +
                    RIGHT('000000' + CONVERT(VARCHAR(20), NEXT VALUE FOR dbo.Seq_EMIReceiptNo), 6);";

            const string insertContraSql = @"
                INSERT INTO EMICollections
                (
                    LoanAccountNo,
                    EMIAmount,
                    PenaltyAmount,
                    CollectionDate,
                    CreatedBy,
                    CreatedOn,
                    InstallmentNumber,
                    ReceiptNumber,
                    PaymentMode,
                    PaymentReference,
                    Remarks,
                    IsReversed,
                    EntryType,
                    ParentCollectionId
                )
                VALUES
                (
                    @LoanAccountNo,
                    @EMIAmount,
                    @PenaltyAmount,
                    @CollectionDate,
                    @CreatedBy,
                    @CreatedOn,
                    @InstallmentNumber,
                    @ReceiptNumber,
                    @PaymentMode,
                    @PaymentReference,
                    @Remarks,
                    0,
                    'Reversal',
                    @ParentCollectionId
                );";

            using var connection = _connectionFactory.CreateConnection();
            await ((System.Data.Common.DbConnection)connection).OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var original = await connection.QueryFirstOrDefaultAsync<EMICollection>(
                    selectOriginalSql,
                    new { CollectionId = collectionId },
                    transaction);

                if (original == null)
                    return false;

                var loanStatus = await connection.QueryFirstOrDefaultAsync<(bool IsLive, int RemainingInstallments)>(
                    loanStatusSql,
                    new { original.LoanAccountNo },
                    transaction);

                if (!loanStatus.IsLive || loanStatus.RemainingInstallments <= 0)
                    return false;

                int updated = await connection.ExecuteAsync(
                    markReversedSql,
                    new
                    {
                        CollectionId = collectionId,
                        Reason = reason,
                        ReversedBy = reversedBy
                    },
                    transaction);

                if (updated == 0)
                    return false;

                string reversalReceiptNumber = await connection.ExecuteScalarAsync<string>(
                    reversalReceiptSql,
                    transaction: transaction) ?? string.Empty;

                string originalRef = string.IsNullOrWhiteSpace(original.ReceiptNumber)
                    ? $"Collection #{original.CollectionId}"
                    : original.ReceiptNumber;

                DateTime nowUtc = DateTime.UtcNow;

                int inserted = await connection.ExecuteAsync(
                    insertContraSql,
                    new
                    {
                        original.LoanAccountNo,
                        EMIAmount = -original.EMIAmount,
                        PenaltyAmount = -original.PenaltyAmount,
                        CollectionDate = DateTime.Today,
                        CreatedBy = reversedBy,
                        CreatedOn = nowUtc,
                        original.InstallmentNumber,
                        ReceiptNumber = reversalReceiptNumber,
                        original.PaymentMode,
                        original.PaymentReference,
                        Remarks = $"Reversal of {originalRef}: {reason}",
                        ParentCollectionId = original.CollectionId
                    },
                    transaction);

                if (inserted == 0)
                    return false;

                transaction.Commit();
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
