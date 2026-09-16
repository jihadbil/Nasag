using System.Threading;
using System.Threading.Tasks;

namespace Nasag.Data;

public interface IDatabaseInitializer
{
    Task<DatabaseInitResult> InitializeAsync(CancellationToken ct = default);
}
