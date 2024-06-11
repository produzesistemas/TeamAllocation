using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace M2P_MFGAllocation.Mappings
{
    internal class ProductionLineMap : IEntityTypeConfiguration<ProductionLine>
    {
        public void Configure(EntityTypeBuilder<ProductionLine> builder)
        {
            builder.ToTable("ProductionLine");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().IsUnicode(false).HasMaxLength(255);
            builder.Property(x => x.SiteId).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();
        }
    }
}