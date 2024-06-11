using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MFGAllocation.Models
{
    public class TeamWorkstationMap : IEntityTypeConfiguration<TeamWorkstation>
    {
        public void Configure(EntityTypeBuilder<TeamWorkstation> builder)
        {
            builder.ToTable("TeamWorkstation");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TeamProductId).IsRequired();
            builder.Property(x => x.WorkstationId).IsRequired();
            builder.HasOne(x => x.Workstation).WithMany().OnDelete(DeleteBehavior.NoAction);
            builder.Navigation(x => x.Workstation).IsRequired();
            builder.HasMany(x => x.TeamWorkstationHCs)
               .WithOne()
               .HasForeignKey(p => p.TeamWorkstationId)
               .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
