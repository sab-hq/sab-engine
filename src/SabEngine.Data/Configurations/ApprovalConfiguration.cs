using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SabEngine.Core;

namespace SabEngine.Data.Configurations;

/// <summary>Table name matches docs/SAB_Design_Document_v0.1.2.md, Section 4.5 exactly: "approvals".</summary>
public sealed class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> builder)
    {
        builder.ToTable("approvals");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.PlanId).IsRequired();

        // Never blank — this is what makes the recommend-and-approve
        // gate auditable, per Section 7 ("who/what approved a plan, not
        // just 'approved'").
        builder.Property(a => a.ApprovedByUserId).IsRequired();

        builder.HasIndex(a => a.PlanId);
    }
}
