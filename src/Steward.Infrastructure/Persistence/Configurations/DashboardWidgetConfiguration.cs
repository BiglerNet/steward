using Steward.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steward.Infrastructure.Persistence.Configurations;

public class DashboardWidgetConfiguration : IEntityTypeConfiguration<DashboardWidget>
{
    public void Configure(EntityTypeBuilder<DashboardWidget> builder)
    {
        builder.ToTable("DashboardWidgets");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.WidgetType).HasConversion<int>();
        builder.Property(w => w.WidgetSize).HasConversion<int>();
        builder.Property(w => w.Config).HasMaxLength(2000);

        builder.HasIndex(w => w.DashboardId);
    }
}
