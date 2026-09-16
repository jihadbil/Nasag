using System.Threading;
using System.Threading.Tasks;

namespace Nasag.Data;

public interface IDbSeeder
{
    /// <summary>
    /// Seeds initial reference data + a demo dataset only when the database is empty.
    /// Idempotent: subsequent calls are no-ops.
    /// </summary>
    Task SeedIfEmptyAsync(CancellationToken ct = default);
}
