using Dapper;
using LoanManagement.DL.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoanManagement.Core.Controllers;

public class DatabaseTestController : Controller
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DatabaseTestController(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IActionResult> Index()
    {
        using var connection = _connectionFactory.CreateConnection();

        var databaseName = await connection.QuerySingleAsync<string>(
            "SELECT DB_NAME()");

        return Content($"Database connection successful. Database: {databaseName}");
    }
}