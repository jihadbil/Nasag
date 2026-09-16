using System.Collections.Generic;
using System.Threading.Tasks;
using NasaqVendor.Models;

namespace NasaqVendor.Repositories;

public interface IIssueAuditRepository
{
    Task<IReadOnlyList<IssueAudit>> ListForLicenseAsync(int licenseId);
    Task<int> InsertAsync(IssueAudit row);
}
