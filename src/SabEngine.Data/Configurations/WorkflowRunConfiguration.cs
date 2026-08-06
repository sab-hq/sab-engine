using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SabEngine.Core;

namespace SabEngine.Data.Configurations;

/// <summary>Table name matches docs/SAB_Design_Document_v0.1.2.md, Section 4.5 exactly: "workflow_runs".</summary>
public sealed class WorkflowRunConfiguration : IEntityTypeConfiguration<WorkflowRun>
{
    public void Configure(EntityTypeBuilder<WorkflowRun> builder)
    {
        builder.ToTable("workflow_runs");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.WorkflowId).IsRequired();
        builder.Property(w => w.Target).IsRequired();
        builder.Property(w => w.State).HasConversion<string>().IsRequired();

        // Frequently queried by Section 4.5's "last N runs of workflow X
        // against target Y" and "what's currently pending my approval".
        builder.HasIndex(w => new { w.WorkflowId, w.Target });
        builder.HasIndex(w => w.State);
    }
}
