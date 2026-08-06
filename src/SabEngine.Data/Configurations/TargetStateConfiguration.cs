using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SabEngine.Core;

namespace SabEngine.Data.Configurations;

/// <summary>Table name matches docs/SAB_Design_Document_v0.1.2.md, Section 4.5 exactly: "target_state".</summary>
public sealed class TargetStateConfiguration : IEntityTypeConfiguration<TargetState>
{
    public void Configure(EntityTypeBuilder<TargetState> builder)
    {
        builder.ToTable("target_state");

        // One row per managed system — Target is the natural key, per
        // Section 4.5: "current known facts per managed system".
        builder.HasKey(t => t.Target);

        // Same reasoning as PlanConfiguration.Steps — Facts is replaced
        // wholesale on each update (Section 4.5: "updated as runs
        // complete"), not mutated key-by-key, so JSON-string comparison
        // is a reasonable starting point.
        var factsComparer = new ValueComparer<IReadOnlyDictionary<string, object?>>(
            (a, b) => JsonSerializer.Serialize(a, JsonSerializerOptions.Default) == JsonSerializer.Serialize(b, JsonSerializerOptions.Default),
            c => JsonSerializer.Serialize(c, JsonSerializerOptions.Default).GetHashCode(),
            c => c);

        builder.Property(t => t.Facts)
            .HasConversion(
                facts => JsonSerializer.Serialize(facts, JsonSerializerOptions.Default),
                json => JsonSerializer.Deserialize<IReadOnlyDictionary<string, object?>>(json, JsonSerializerOptions.Default)!)
            .Metadata.SetValueComparer(factsComparer);

        builder.Property(t => t.Facts).HasColumnType("jsonb").IsRequired();
    }
}
