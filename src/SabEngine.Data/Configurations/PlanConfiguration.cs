using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SabEngine.Core;

namespace SabEngine.Data.Configurations;

/// <summary>Table name matches docs/SAB_Design_Document_v0.1.2.md, Section 4.5 exactly: "plans".</summary>
public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.WorkflowRunId).IsRequired();
        builder.Property(p => p.Reasoning).IsRequired();

        // Section 4.1: a Plan is a typed, structured object (not free
        // text) — but the module sequence itself is naturally
        // variable-shaped, so it's stored as jsonb rather than forced
        // into relational columns. This is a scaffolding-stage choice;
        // revisit if per-step querying is ever needed (see PD-3 notes
        // in pre-development-checklist.md).
        //
        // A Plan's Steps are set once at creation and never mutated in
        // place (Section 4.1's model treats a Plan as immutable once
        // drafted), so comparing by re-serialized JSON is a reasonable
        // starting point rather than a per-element structural comparer —
        // simple, correct, and fine at this scale. Revisit if Steps ever
        // needs to support large collections or in-place mutation.
        var stepsComparer = new ValueComparer<IReadOnlyList<ProposedModuleStep>>(
            (a, b) => JsonSerializer.Serialize(a, JsonSerializerOptions.Default) == JsonSerializer.Serialize(b, JsonSerializerOptions.Default),
            c => JsonSerializer.Serialize(c, JsonSerializerOptions.Default).GetHashCode(),
            c => c);

        builder.Property(p => p.Steps)
            .HasConversion(
                steps => JsonSerializer.Serialize(steps, JsonSerializerOptions.Default),
                json => JsonSerializer.Deserialize<IReadOnlyList<ProposedModuleStep>>(json, JsonSerializerOptions.Default)!)
            .Metadata.SetValueComparer(stepsComparer);

        builder.Property(p => p.Steps).HasColumnType("jsonb").IsRequired();
    }
}
