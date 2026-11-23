
using Dapper;
using LoanManagement.DL.Interfaces;
using LoanManagement.DL.Models;

namespace LoanManagement.DL.Interfaces;

public interface ICustomerRepository
{
    Task<bool> IsAadhaarExistsAsync(string aadhaarNumber);

    Task<bool> IsPANExistsAsync(string panNumber);

    Task<int> GetActiveLoanCountAsync(
        string aadhaarNumber,
        string panNumber,
        string customerName);

    Task<bool> IsSameCustomerAsync(
    string aadhaarNumber,
    string panNumber,
    string customerName);

    Task<string> RegisterCustomerAsync(Customer customer);
}
