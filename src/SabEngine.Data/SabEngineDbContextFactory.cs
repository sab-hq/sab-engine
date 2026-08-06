using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SabEngine.Data;

/// <summary>
/// Lets `dotnet ef migrations add`/`dotnet ef database update` construct
/// a SabEngineDbContext without needing the full app's DI host running.
/// Reads the same connection string the app itself would use — see
/// SABENGINE_CONNECTIONSTRING in docker-compose.yml and
/// src/SabEngine.Api/appsettings.json.
///
/// The default here matches docker-compose.yml's local dev credentials
/// exactly — not a real secret, and never meant to be one. See the
/// warning in docker-compose.yml itself.
/// </summary>
public sealed class SabEngineDbContextFactory : IDesignTimeDbContextFactory<SabEngineDbContext>
{
    public SabEngineDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SABENGINE_CONNECTIONSTRING")
            ?? "Host=localhost;Port=5433;Database=sabengine;Username=sabengine;Password=sabengine_dev_only";

        var optionsBuilder = new DbContextOptionsBuilder<SabEngineDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new SabEngineDbContext(optionsBuilder.Options);
    }
}
