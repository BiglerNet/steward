using Steward.Domain.Entities;
using Steward.Domain.Entities.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Steward.Infrastructure.Persistence.Configurations;

public class MileageLogConfiguration : IEntityTypeConfiguration<MileageLog>
{
    public void Configure(EntityTypeBuilder<MileageLog> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(m => m.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.AssetId);
    }
}
