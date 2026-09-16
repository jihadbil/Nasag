using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using NasaqVendor.Database;
using NasaqVendor.Models;

namespace NasaqVendor.Repositories;

public sealed class CustomersRepository : ICustomersRepository
{
    private readonly VendorDb _db;

    public CustomersRepository(VendorDb db) => _db = db;

    public async Task<IReadOnlyList<Customer>> ListAsync(string? search = null)
    {
        using var c = _db.CreateConnection();
        const string sql = @"
SELECT Id, Code, Name, Phone, Email, City, Notes, CreatedAtUtc
FROM Customers
WHERE (@search IS NULL OR @search = '' OR Name LIKE '%' || @search || '%' OR Code LIKE '%' || @search || '%' OR IFNULL(Phone,'') LIKE '%' || @search || '%' OR IFNULL(City,'') LIKE '%' || @search || '%')
ORDER BY CreatedAtUtc DESC;";
        var rows = await c.QueryAsync<Customer>(sql, new { search });
        return rows.ToList();
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        using var c = _db.CreateConnection();
        const string sql = @"SELECT Id, Code, Name, Phone, Email, City, Notes, CreatedAtUtc FROM Customers WHERE Id = @id;";
        return await c.QuerySingleOrDefaultAsync<Customer>(sql, new { id });
    }

    public async Task<int> InsertAsync(Customer cust)
    {
        using var c = _db.CreateConnection();
        const string sql = @"
INSERT INTO Customers (Code, Name, Phone, Email, City, Notes, CreatedAtUtc)
VALUES (@Code, @Name, @Phone, @Email, @City, @Notes, @CreatedAtUtc);
SELECT last_insert_rowid();";
        cust.CreatedAtUtc = cust.CreatedAtUtc == default ? DateTime.UtcNow : cust.CreatedAtUtc;
        var id = await c.ExecuteScalarAsync<long>(sql, cust);
        cust.Id = (int)id;
        return cust.Id;
    }

    public async Task UpdateAsync(Customer cust)
    {
        using var c = _db.CreateConnection();
        const string sql = @"
UPDATE Customers
SET Code = @Code, Name = @Name, Phone = @Phone, Email = @Email, City = @City, Notes = @Notes
WHERE Id = @Id;";
        await c.ExecuteAsync(sql, cust);
    }

    public async Task DeleteAsync(int id)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync(@"DELETE FROM Customers WHERE Id = @id;", new { id });
    }

    public async Task<int> CountLicensesAsync(int customerId)
    {
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>(
            @"SELECT COUNT(1) FROM Licenses WHERE CustomerId = @customerId;",
            new { customerId });
    }

    public async Task<string> NextCodeAsync()
    {
        using var c = _db.CreateConnection();
        const string sql = @"SELECT IFNULL(MAX(CAST(SUBSTR(Code, 6) AS INTEGER)), 0) FROM Customers WHERE Code LIKE 'CUST-%';";
        var n = await c.ExecuteScalarAsync<long?>(sql) ?? 0;
        return $"CUST-{(n + 1):0000}";
    }
}
