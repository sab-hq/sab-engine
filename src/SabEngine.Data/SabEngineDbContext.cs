using Microsoft.EntityFrameworkCore;
using SabEngine.Core;

namespace SabEngine.Data;

/// <summary>
/// The Engine State Store (ESS) — SabEngine's own memory. See
/// docs/SAB_Design_Document_v0.1.2.md, Section 4.5, and
/// engine-state-store.md for the plain-language version.
///
/// Table names deliberately match the design doc's Section 4.5 data
/// model exactly (snake_case, as written there) rather than defaulting
/// to EF Core's PascalCase convention — see SabEngineDbContextConfiguration
/// for the mapping.
/// </summary>
public sealed class SabEngineDbContext : DbContext
{
    public SabEngineDbContext(DbContextOptions<SabEngineDbContext> options) : base(options) { }

    public DbSet<WorkflowRun> WorkflowRuns => Set<WorkflowRun>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<ExecutionResult> ExecutionResults => Set<ExecutionResult>();
    public DbSet<TargetState> TargetStates => Set<TargetState>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SabEngineDbContext).Assembly);
    }
}
