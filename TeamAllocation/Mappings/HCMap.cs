using MFGAllocation.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MFGAllocation.Mappings
{
    public class HCMap : IEntityTypeConfiguration<HC>
    {
        public void Configure(EntityTypeBuilder<HC> builder)
        {
            builder.ToTable("HC");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsUnicode(false).HasMaxLength(255);
            builder.Property(x => x.Workday).IsUnicode(false).HasMaxLength(255);
            builder.Property(x => x.SiteCode).IsUnicode(false).HasMaxLength(3);
            builder.HasOne(x => x.Shift);
            builder.HasOne(x => x.Site);
            builder.HasOne(x => x.Statuses);
            builder.HasMany(x => x.HCTrainnings)
                .WithOne()
                .HasForeignKey(h => h.HCId);
        }
    }

}
