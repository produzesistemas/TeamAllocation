using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MFGAllocation.Mappings
{
    internal class TeamProductMap : IEntityTypeConfiguration<TeamProduct>
    {
        public void Configure(EntityTypeBuilder<TeamProduct> builder)
        {
            builder.ToTable("TeamProduct");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ProductId).IsRequired();
            builder.HasOne(x => x.Product).WithMany().OnDelete(DeleteBehavior.NoAction);
            builder.Navigation(x => x.Product).IsRequired();
            builder.Property(x => x.Category).IsRequired(false).IsUnicode(false).HasMaxLength(255);
            builder.Property(x => x.TeamId).IsRequired();
            builder.HasMany(x => x.TeamWorkstations)
              .WithOne()
              .HasForeignKey(p => p.TeamProductId);
            builder.Property(x=>x.LayoutId).IsRequired();
            builder.HasOne(x => x.Layout);
            builder.Navigation(x=>x.Layout).IsRequired();
                
        }
    }
}