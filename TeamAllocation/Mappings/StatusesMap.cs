using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MFGAllocation.Mappings
{
    public class StatusesMap : IEntityTypeConfiguration<Statuses>
    {
        public void Configure(EntityTypeBuilder<Statuses> builder)
        {
            builder.ToTable("Statuses");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsUnicode(false).HasMaxLength(255);
            builder.Property(x => x.SiteCode).IsUnicode(false).HasMaxLength(3);
        }
    }
}
