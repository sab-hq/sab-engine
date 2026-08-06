using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SabEngine.Core;

namespace SabEngine.Data.Configurations;

/// <summary>
/// Table name "audit_entries" — not given a literal snake_case name in
/// docs/SAB_Design_Document_v0.1.2.md, Section 4.5's data model list,
/// but follows the same pluralized-snake_case pattern as the other six
/// tables there.
///
/// Written once, never updated (Section 7, "Tamper-evidence, made
/// concrete") — there is deliberately no update path configured here.
/// </summary>
public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.WorkflowRunId).IsRequired();
        builder.Property(a => a.FromState).HasConversion<string>().IsRequired();
        builder.Property(a => a.ToState).HasConversion<string>().IsRequired();
        builder.Property(a => a.Actor).IsRequired();
        builder.Property(a => a.Hash).IsRequired();

        builder.HasIndex(a => a.WorkflowRunId);
        builder.HasIndex(a => a.Timestamp);
    }
}
