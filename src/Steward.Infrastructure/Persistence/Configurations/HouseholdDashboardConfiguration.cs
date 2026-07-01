using Steward.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steward.Infrastructure.Persistence.Configurations;

public class HouseholdDashboardConfiguration : IEntityTypeConfiguration<HouseholdDashboard>
{
    public void Configure(EntityTypeBuilder<HouseholdDashboard> builder)
    {
        builder.ToTable("HouseholdDashboards");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(100);

        builder.HasOne<Household>()
            .WithMany()
            .HasForeignKey(d => d.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.HouseholdId);

        builder.HasIndex(d => new { d.HouseholdId, d.Name })
            .IsUnique()
            .HasDatabaseName("IX_HouseholdDashboards_HouseholdId_Name");

        builder.HasMany(d => d.Widgets)
            .WithOne(w => w.Dashboard)
            .HasForeignKey(w => w.DashboardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
