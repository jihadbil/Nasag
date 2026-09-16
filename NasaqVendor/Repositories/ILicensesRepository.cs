using System.Collections.Generic;
using System.Threading.Tasks;
using NasaqVendor.Models;

namespace NasaqVendor.Repositories;

public interface ILicensesRepository
{
    Task<IReadOnlyList<LicenseRecord>> ListAsync(string? search = null, string? statusFilter = null);
    Task<LicenseRecord?> GetByIdAsync(int id);
    Task<int> InsertAsync(LicenseRecord rec);
    Task RevokeAsync(int id);
    Task UpdateLicenseFilePathAsync(int id, string newPath);
    Task<(int Active, int Revoked)> GetCountsAsync();
}
