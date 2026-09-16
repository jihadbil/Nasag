using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Nasag.Data;

/// <summary>
/// Used by <c>dotnet ef</c> at design time so migrations can be added/applied without spinning up the full WPF host.
/// </summary>
public sealed class NasaqDbContextFactory : IDesignTimeDbContextFactory<NasaqDbContext>
{
    public NasaqDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var cs = config.GetConnectionString("DefaultConnection")
                 ?? "Server=(localdb)\\MSSQLLocalDB;Database=NasaqSchoolDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

        var options = new DbContextOptionsBuilder<NasaqDbContext>()
            .UseSqlServer(cs, sql => sql.MigrationsAssembly(typeof(NasaqDbContext).Assembly.GetName().Name))
            .Options;

        return new NasaqDbContext(options);
    }
}
