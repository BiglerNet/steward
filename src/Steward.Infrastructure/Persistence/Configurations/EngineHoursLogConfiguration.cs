using Steward.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steward.Infrastructure.Persistence.Configurations;

public class EngineHoursLogConfiguration : IEntityTypeConfiguration<EngineHoursLog>
{
    public void Configure(EntityTypeBuilder<EngineHoursLog> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne<Engine>()
            .WithMany()
            .HasForeignKey(e => e.EngineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.EngineId);
    }
}
