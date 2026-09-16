namespace Nasag.Data;

public enum DatabaseInitStatus
{
    Success = 0,
    CannotConnect = 1,
    MigrationFailed = 2,
    SeedFailed = 3,
    Unknown = 99
}

public sealed class DatabaseInitResult
{
    public DatabaseInitStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Details { get; init; }
    public int AppliedMigrationsCount { get; init; }

    public bool IsSuccess => Status == DatabaseInitStatus.Success;

    public static DatabaseInitResult Ok(int applied) => new()
    {
        Status = DatabaseInitStatus.Success,
        AppliedMigrationsCount = applied
    };

    public static DatabaseInitResult Fail(DatabaseInitStatus status, string message, string? details = null) => new()
    {
        Status = status,
        ErrorMessage = message,
        Details = details
    };
}
