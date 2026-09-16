using System.Collections.Generic;
using System.Threading.Tasks;
using NasaqVendor.Models;

namespace NasaqVendor.Repositories;

public interface ICustomersRepository
{
    Task<IReadOnlyList<Customer>> ListAsync(string? search = null);
    Task<Customer?> GetByIdAsync(int id);
    Task<int> InsertAsync(Customer c);
    Task UpdateAsync(Customer c);
    Task DeleteAsync(int id);
    Task<int> CountLicensesAsync(int customerId);
    Task<string> NextCodeAsync();
}
