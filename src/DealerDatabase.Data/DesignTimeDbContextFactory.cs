using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DealerDatabase.Data;

/// <summary>
/// Used by the EF Core tools (e.g. <c>dotnet ef migrations add</c>) to create the context at design time.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DealerDbContext>
{
    public DealerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DealerDbContext>()
            .UseSqlite($"Data Source={SolutionPaths.DatabaseFile}")
            .Options;

        return new DealerDbContext(options);
    }
}
