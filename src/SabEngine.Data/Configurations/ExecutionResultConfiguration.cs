using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SabEngine.Core;

namespace SabEngine.Data.Configurations;

/// <summary>Table name matches docs/SAB_Design_Document_v0.1.2.md, Section 4.5 exactly: "execution_results".</summary>
public sealed class ExecutionResultConfiguration : IEntityTypeConfiguration<ExecutionResult>
{
    public void Configure(EntityTypeBuilder<ExecutionResult> builder)
    {
        builder.ToTable("execution_results");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.WorkflowRunId).IsRequired();
        builder.Property(e => e.ModuleId).IsRequired();

        builder.HasIndex(e => e.WorkflowRunId);
    }
}
