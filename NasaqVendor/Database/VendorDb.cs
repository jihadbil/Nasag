using System;
using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;
using NasaqVendor.Helpers;

namespace NasaqVendor.Database;

/// <summary>
/// SQLite connection factory + schema bootstrap.
/// </summary>
public sealed class VendorDb
{
    private readonly string _connectionString;
    private bool _initialized;
    private readonly object _gate = new();

    public VendorDb()
    {
        var dir = Path.GetDirectoryName(PathProvider.DatabaseFile);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var b = new SqliteConnectionStringBuilder
        {
            DataSource = PathProvider.DatabaseFile,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };
        _connectionString = b.ConnectionString;
    }

    public string DatabaseFilePath => PathProvider.DatabaseFile;

    public IDbConnection CreateConnection()
    {
        EnsureSchema();
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using (var pragma = conn.CreateCommand())
        {
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
        }
        return conn;
    }

    private void EnsureSchema()
    {
        if (_initialized) return;
        lock (_gate)
        {
            if (_initialized) return;
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = SchemaSql;
            cmd.ExecuteNonQuery();
            _initialized = true;
        }
    }

    private const string SchemaSql = @"
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Customers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Code TEXT UNIQUE NOT NULL,
    Name TEXT NOT NULL,
    Phone TEXT,
    Email TEXT,
    City TEXT,
    Notes TEXT,
    CreatedAtUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Licenses (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CustomerId INTEGER NOT NULL,
    Edition TEXT NOT NULL,
    FeaturesJson TEXT NOT NULL,
    MachineHashesJson TEXT NOT NULL,
    IssuedAtUtc TEXT NOT NULL,
    ExpiresAtUtc TEXT,
    LicenseFilePath TEXT NOT NULL,
    Revoked INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
);

CREATE INDEX IF NOT EXISTS IX_Licenses_CustomerId ON Licenses(CustomerId);

CREATE TABLE IF NOT EXISTS IssueAudit (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    LicenseId INTEGER NOT NULL,
    Action TEXT NOT NULL,
    AtUtc TEXT NOT NULL,
    Operator TEXT,
    Notes TEXT,
    FOREIGN KEY (LicenseId) REFERENCES Licenses(Id)
);

CREATE INDEX IF NOT EXISTS IX_IssueAudit_LicenseId ON IssueAudit(LicenseId);
";
}
