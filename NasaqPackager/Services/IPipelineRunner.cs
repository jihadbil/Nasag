using System.Collections.Generic;
using System.Threading;

namespace NasaqPackager.Services;

public sealed record PipelineConfig(
    string ProjectPath,
    string PackId,
    string PackTitle,
    string Version,
    string ReleasesPath,
    string Channel,
    string IconPath,
    string Rid,
    bool SelfContained,
    bool RunObfuscar);

public interface IPipelineRunner
{
    IAsyncEnumerable<string> RunPipelineAsync(PipelineConfig cfg, CancellationToken ct);
}
