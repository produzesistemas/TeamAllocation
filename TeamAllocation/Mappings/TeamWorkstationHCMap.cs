using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MFGAllocation.Models
{
    public class TeamWorkstationHCMap : IEntityTypeConfiguration<TeamWorkstationHC>
    {
        public void Configure(EntityTypeBuilder<TeamWorkstationHC> builder)
        {
            builder.ToTable("TeamWorkstationHC");
            builder.HasKey("TeamWorkstationId", "HCId");
            builder.Property(x => x.TeamWorkstationId).IsRequired();
            builder.Property(x => x.HCId).IsRequired();
        }
    }
}
