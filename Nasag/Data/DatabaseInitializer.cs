using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Nasag.Data;

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbContextFactory<NasaqDbContext> _factory;
    private readonly IDbSeeder _seeder;

    public DatabaseInitializer(IDbContextFactory<NasaqDbContext> factory, IDbSeeder seeder)
    {
        _factory = factory;
        _seeder = seeder;
    }

    public async Task<DatabaseInitResult> InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await using var ctx = await _factory.CreateDbContextAsync(ct).ConfigureAwait(false);

            // 1) Pending migrations — apply them all (works for first-time create as well).
            var pending = (await ctx.Database.GetPendingMigrationsAsync(ct).ConfigureAwait(false)).ToList();

            if (pending.Count > 0)
            {
                try
                {
                    await ctx.Database.MigrateAsync(ct).ConfigureAwait(false);
                }
                catch (Exception migEx)
                {
                    return DatabaseInitResult.Fail(
                        DatabaseInitStatus.MigrationFailed,
                        "تعذّر تحديث قاعدة البيانات.",
                        migEx.Message);
                }
            }
            else
            {
                // Make sure we can actually reach the existing DB.
                var canConnect = await ctx.Database.CanConnectAsync(ct).ConfigureAwait(false);
                if (!canConnect)
                {
                    return DatabaseInitResult.Fail(
                        DatabaseInitStatus.CannotConnect,
                        "تعذّر الاتصال بقاعدة البيانات. تحقق من تشغيل SQL Server وصحة سلسلة الاتصال.");
                }
            }

            // 2) Seed data when empty.
            try
            {
                await _seeder.SeedIfEmptyAsync(ct).ConfigureAwait(false);
            }
            catch (Exception seedEx)
            {
                return DatabaseInitResult.Fail(
                    DatabaseInitStatus.SeedFailed,
                    "تعذّر تحميل البيانات الأولية.",
                    seedEx.Message);
            }

            return DatabaseInitResult.Ok(pending.Count);
        }
        catch (SqlException sqlEx)
        {
            return DatabaseInitResult.Fail(
                DatabaseInitStatus.CannotConnect,
                "تعذّر الاتصال بقاعدة البيانات. تحقق من تشغيل SQL Server وصحة سلسلة الاتصال.",
                sqlEx.Message);
        }
        catch (Exception ex)
        {
            return DatabaseInitResult.Fail(
                DatabaseInitStatus.Unknown,
                "حدث خطأ غير متوقع أثناء تهيئة قاعدة البيانات.",
                ex.Message);
        }
    }
}
