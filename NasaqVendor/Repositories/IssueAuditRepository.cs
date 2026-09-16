using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using NasaqVendor.Database;
using NasaqVendor.Models;

namespace NasaqVendor.Repositories;

public sealed class IssueAuditRepository : IIssueAuditRepository
{
    private readonly VendorDb _db;

    public IssueAuditRepository(VendorDb db) => _db = db;

    public async Task<IReadOnlyList<IssueAudit>> ListForLicenseAsync(int licenseId)
    {
        using var c = _db.CreateConnection();
        const string sql = @"
SELECT Id, LicenseId, Action, AtUtc, Operator, Notes
FROM IssueAudit
WHERE LicenseId = @licenseId
ORDER BY AtUtc DESC;";
        var rows = await c.QueryAsync<IssueAudit>(sql, new { licenseId });
        return rows.ToList();
    }

    public async Task<int> InsertAsync(IssueAudit row)
    {
        using var c = _db.CreateConnection();
        const string sql = @"
INSERT INTO IssueAudit (LicenseId, Action, AtUtc, Operator, Notes)
VALUES (@LicenseId, @Action, @AtUtc, @Operator, @Notes);
SELECT last_insert_rowid();";
        row.AtUtc = row.AtUtc == default ? DateTime.UtcNow : row.AtUtc;
        var id = await c.ExecuteScalarAsync<long>(sql, row);
        row.Id = (int)id;
        return row.Id;
    }
}
