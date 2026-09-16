using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using NasaqVendor.Database;
using NasaqVendor.Models;

namespace NasaqVendor.Repositories;

public sealed class LicensesRepository : ILicensesRepository
{
    private readonly VendorDb _db;

    public LicensesRepository(VendorDb db) => _db = db;

    public async Task<IReadOnlyList<LicenseRecord>> ListAsync(string? search = null, string? statusFilter = null)
    {
        using var c = _db.CreateConnection();
        var sql = @"
SELECT L.Id, L.CustomerId, L.Edition, L.FeaturesJson, L.MachineHashesJson,
       L.IssuedAtUtc, L.ExpiresAtUtc, L.LicenseFilePath, L.Revoked,
       C.Name AS CustomerName, C.Code AS CustomerCode
FROM Licenses L
INNER JOIN Customers C ON C.Id = L.CustomerId
WHERE (@search IS NULL OR @search = '' OR C.Name LIKE '%' || @search || '%' OR C.Code LIKE '%' || @search || '%')";

        if (string.Equals(statusFilter, "Active", StringComparison.OrdinalIgnoreCase))
            sql += " AND L.Revoked = 0";
        else if (string.Equals(statusFilter, "Revoked", StringComparison.OrdinalIgnoreCase))
            sql += " AND L.Revoked = 1";

        sql += " ORDER BY L.IssuedAtUtc DESC;";

        var rows = await c.QueryAsync<LicenseRecord>(sql, new { search });
        return rows.ToList();
    }

    public async Task<LicenseRecord?> GetByIdAsync(int id)
    {
        using var c = _db.CreateConnection();
        const string sql = @"
SELECT L.Id, L.CustomerId, L.Edition, L.FeaturesJson, L.MachineHashesJson,
       L.IssuedAtUtc, L.ExpiresAtUtc, L.LicenseFilePath, L.Revoked,
       C.Name AS CustomerName, C.Code AS CustomerCode
FROM Licenses L
INNER JOIN Customers C ON C.Id = L.CustomerId
WHERE L.Id = @id;";
        return await c.QuerySingleOrDefaultAsync<LicenseRecord>(sql, new { id });
    }

    public async Task<int> InsertAsync(LicenseRecord rec)
    {
        using var c = _db.CreateConnection();
        const string sql = @"
INSERT INTO Licenses (CustomerId, Edition, FeaturesJson, MachineHashesJson, IssuedAtUtc, ExpiresAtUtc, LicenseFilePath, Revoked)
VALUES (@CustomerId, @EditionText, @FeaturesJson, @MachineHashesJson, @IssuedAtUtc, @ExpiresAtUtc, @LicenseFilePath, @RevokedInt);
SELECT last_insert_rowid();";
        var id = await c.ExecuteScalarAsync<long>(sql, new
        {
            rec.CustomerId,
            EditionText = rec.Edition.ToString(),
            rec.FeaturesJson,
            rec.MachineHashesJson,
            rec.IssuedAtUtc,
            rec.ExpiresAtUtc,
            rec.LicenseFilePath,
            RevokedInt = rec.Revoked ? 1 : 0
        });
        rec.Id = (int)id;
        return rec.Id;
    }

    public async Task RevokeAsync(int id)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync(@"UPDATE Licenses SET Revoked = 1 WHERE Id = @id;", new { id });
    }

    public async Task UpdateLicenseFilePathAsync(int id, string newPath)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync(@"UPDATE Licenses SET LicenseFilePath = @newPath WHERE Id = @id;", new { id, newPath });
    }

    public async Task<(int Active, int Revoked)> GetCountsAsync()
    {
        using var c = _db.CreateConnection();
        const string sql = @"
SELECT
    SUM(CASE WHEN Revoked = 0 THEN 1 ELSE 0 END) AS Active,
    SUM(CASE WHEN Revoked = 1 THEN 1 ELSE 0 END) AS Revoked
FROM Licenses;";
        var r = await c.QuerySingleOrDefaultAsync<(int? Active, int? Revoked)>(sql);
        return (r.Active ?? 0, r.Revoked ?? 0);
    }
}
