using Microsoft.EntityFrameworkCore;
using Npgsql;
using SabEngine.Data;

namespace SabEngine.Orchestration.Tests;

/// <summary>
/// <see cref="WorkflowRunClaimService"/> relies on EF Core's
/// <c>ExecuteUpdateAsync</c> performing a real, atomic, conditional SQL
/// <c>UPDATE</c> — a guarantee the InMemory test provider simply doesn't
/// have (confirmed: it throws <c>InvalidOperationException</c>, "could
/// not be translated," when Brock ran these tests against InMemory).
/// Testing atomicity against a provider with no real atomicity concept
/// would prove nothing even if it happened to pass, so these tests run
/// against a real, disposable Postgres database instead — created fresh
/// per test and dropped afterward, against the same local Docker
/// Postgres from PD-3 (see docker-compose.yml, port 5433).
///
/// This means Docker must be running locally
/// (<c>docker compose up -d</c> from the repo root) for these specific
/// tests to pass — a deliberate trade-off, not an oversight. See
/// pre-development-checklist.md, PD-5.
/// </summary>
public sealed class PostgresTestDatabase : IAsyncDisposable
{
    private const string AdminConnectionString = "Host=localhost;Port=5433;Database=postgres;Username=sabengine;Password=sabengine_dev_only";
    private readonly string _databaseName = $"sabengine_test_{Guid.NewGuid():N}";

    public SabEngineDbContext Context { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await using (var admin = new NpgsqlConnection(AdminConnectionString))
        {
            await admin.OpenAsync();
            // CREATE DATABASE can't run inside a transaction — a plain
            // connection (not EF Core, which wraps things) is required.
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        var connectionString = $"Host=localhost;Port=5433;Database={_databaseName};Username=sabengine;Password=sabengine_dev_only";
        var options = new DbContextOptionsBuilder<SabEngineDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        Context = new SabEngineDbContext(options);
        // EnsureCreated (not a real migration) is fine here — these
        // tests only need the schema shape to exist, not to validate the
        // migration itself (that's PD-3's job).
        await Context.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();

        await using var admin = new NpgsqlConnection(AdminConnectionString);
        await admin.OpenAsync();

        // Terminate any lingering connections first, or DROP DATABASE fails.
        await using (var terminate = new NpgsqlCommand(
            $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{_databaseName}' AND pid <> pg_backend_pid()", admin))
        {
            await terminate.ExecuteNonQueryAsync();
        }

        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\"", admin);
        await drop.ExecuteNonQueryAsync();
    }
}
