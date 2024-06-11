using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MFGAllocation.Mappings
{
    public class HCTrainningMap : IEntityTypeConfiguration<HCTrainning>
    {
        public void Configure(EntityTypeBuilder<HCTrainning> builder)
        {
            builder.ToTable("HCTrainning");
            builder.HasKey("HCId", "WorkstationId", "LayoutId");
        }
    }

}
