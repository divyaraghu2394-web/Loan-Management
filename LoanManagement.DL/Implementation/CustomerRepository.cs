
using Dapper;
using LoanManagement.DL.Models;
using LoanManagement.DL.Interfaces;

namespace LoanManagement.DL.Implementation;

public class CustomerRepository : ICustomerRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CustomerRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> IsAadhaarExistsAsync(string aadhaarNumber)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.Customers
                WHERE AadhaarNumber = @AadhaarNumber
            )
            THEN 1 ELSE 0 END;
            """;

        return await connection.ExecuteScalarAsync<bool>(
            sql, new { AadhaarNumber = aadhaarNumber });
    }

    public async Task<bool> IsPANExistsAsync(string panNumber)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.Customers
                WHERE PANNumber = @PANNumber
            )
            THEN 1 ELSE 0 END;
            """;

        return await connection.ExecuteScalarAsync<bool>(
            sql, new { PANNumber = panNumber });
    }

    public async Task<int> GetActiveLoanCountAsync(
        string aadhaarNumber,
        string panNumber,
        string customerName)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            SELECT COUNT(*)
            FROM dbo.Customers
            WHERE AadhaarNumber = @AadhaarNumber
              AND PANNumber = @PANNumber;
            """;

        return await connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                AadhaarNumber = aadhaarNumber,
                PANNumber = panNumber,
                CustomerName = customerName
            });
    }

    public async Task<string> RegisterCustomerAsync(Customer customer)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
            INSERT INTO dbo.Customers
            (
                CustomerName,
                AadhaarNumber,
                PANNumber,
                SignatureImagePath,
                PhotoPath,
                AadhaarDocumentPath,
                PANDocumentPath,
                ReferredPerson,
                ReferredContactNo,
                PrimaryContactNo,
                SecondaryContactNo,
                LoanAmount,
                EMIAmount,
                InstallmentCount,
                IsLive,
                CreatedBy,
                Remarks
            )
            OUTPUT INSERTED.LoanAccountNo
            VALUES
            (
                @CustomerName,
                @AadhaarNumber,
                @PANNumber,
                @SignatureImagePath,
                @PhotoPath,
                @AadhaarDocumentPath,
                @PANDocumentPath,
                @ReferredPerson,
                @ReferredContactNo,
                @PrimaryContactNo,
                @SecondaryContactNo,
                @LoanAmount,
                @EMIAmount,
                @InstallmentCount,
                1,
                @CreatedBy,
                @Remarks
            );
            """;

        return await connection.ExecuteScalarAsync<string>(
            sql, customer);
    }

    public async Task<bool> IsSameCustomerAsync(
    string aadhaarNumber,
    string panNumber,
    string customerName)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = """
        SELECT CASE WHEN EXISTS
        (
            SELECT 1
            FROM dbo.Customers
            WHERE AadhaarNumber = @AadhaarNumber
              AND PANNumber = @PANNumber
        )
        THEN 1 ELSE 0 END;
        """;

        return await connection.ExecuteScalarAsync<bool>(
            sql,
            new
            {
                AadhaarNumber = aadhaarNumber,
                PANNumber = panNumber,
                CustomerName = customerName
            });
    }
}
