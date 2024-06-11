using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MFGAllocation.Mappings
{
    public class HistoricChangePlanMap : IEntityTypeConfiguration<HistoricChange>
    {
        public void Configure(EntityTypeBuilder<HistoricChange> builder)
        {
            builder.ToTable("HistoricChange");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Model).IsUnicode(false).HasMaxLength(255);
            builder.Property(x => x.ModelId).IsUnicode(false).HasMaxLength(255);
            builder.Property(x => x.Reason).IsUnicode(false).HasMaxLength(4000);
            builder.Property(x => x.Date);
            builder.Property(x => x.User).IsUnicode(false).HasMaxLength(255);

        }
    }
}
