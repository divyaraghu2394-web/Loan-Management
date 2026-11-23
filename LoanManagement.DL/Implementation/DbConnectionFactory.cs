using LoanManagement.DL.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace LoanManagement.DL.Implementation;

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}