using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.Sql;
using System.Threading;
using System.Threading.Tasks;

namespace Nasag.Services;

/// <summary>
/// تنفيذ <see cref="IServerDiscoveryService"/> يعتمد على
/// <see cref="SqlDataSourceEnumerator"/> مع timeout مضمون لمنع تجميد الواجهة.
/// </summary>
public sealed class ServerDiscoveryService : IServerDiscoveryService
{
    private const int DiscoveryTimeoutMs = 6000;

    public async Task<IReadOnlyList<DiscoveredServer>> DiscoverAsync(CancellationToken ct = default)
    {
        var results = new List<DiscoveredServer>();

        // إدخالات احتياطية دائمة الحضور.
        results.Add(new DiscoveredServer(
            "LocalDB (الجهاز المحلي)",
            @"(localdb)\MSSQLLocalDB",
            IsLocal: true));

        results.Add(new DiscoveredServer(
            "الخادم المحلي (.)",
            ".",
            IsLocal: true));

        results.Add(new DiscoveredServer(
            "SQL Server Express (.\\SQLEXPRESS)",
            ".\\SQLEXPRESS",
            IsLocal: true));

        try
        {
            // SqlDataSourceEnumerator.GetDataSources يحجب، لذلك نشغّله في Task.Run
            // ونعطيه سقف 6 ثوانٍ — إن لم ينتهِ نعيد المتوفر فقط.
            var discoveryTask = Task.Run(() =>
            {
                try
                {
                    return SqlDataSourceEnumerator.Instance.GetDataSources();
                }
                catch
                {
                    return null;
                }
            }, ct);

            var winner = await Task.WhenAny(discoveryTask, Task.Delay(DiscoveryTimeoutMs, ct))
                .ConfigureAwait(false);

            if (winner == discoveryTask)
            {
                var table = await discoveryTask.ConfigureAwait(false);
                if (table is not null)
                {
                    var machine = Environment.MachineName ?? string.Empty;
                    foreach (DataRow row in table.Rows)
                    {
                        var serverName = (row["ServerName"] as string ?? string.Empty).Trim();
                        var instanceName = (row["InstanceName"] as string ?? string.Empty).Trim();

                        if (string.IsNullOrWhiteSpace(serverName)) continue;

                        var target = string.IsNullOrWhiteSpace(instanceName)
                            ? serverName
                            : $"{serverName}\\{instanceName}";

                        var display = target;
                        var isLocal = string.Equals(serverName, machine, StringComparison.OrdinalIgnoreCase);

                        results.Add(new DiscoveredServer(display, target, isLocal));
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // أي خطأ أثناء الاكتشاف نتجاهله ونكتفي بالإدخالات الاحتياطية.
        }

        // إزالة التكرار حسب ConnectionTarget (case-insensitive)، ثم الترتيب:
        // المحلي أولاً، ثم أبجدياً حسب الاسم الظاهر.
        var deduped = results
            .GroupBy(r => r.ConnectionTarget, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderByDescending(r => r.IsLocal)
            .ThenBy(r => r.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return deduped;
    }
}
