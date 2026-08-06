using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SabEngine.Core;

namespace SabEngine.Data.Configurations;

/// <summary>Table name matches docs/SAB_Design_Document_v0.1.2.md, Section 4.5 exactly: "notes".</summary>
public sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("notes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Target).IsRequired();
        builder.Property(n => n.Text).IsRequired();
        builder.Property(n => n.AuthorUserId).IsRequired();

        // Per Section 4.5: "give me any human notes attached to target Y".
        builder.HasIndex(n => n.Target);
    }
}
