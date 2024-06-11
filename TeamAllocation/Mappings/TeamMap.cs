using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MFGAllocation.Mappings
{
    internal class TeamMap : IEntityTypeConfiguration<Team>
    {
        public void Configure(EntityTypeBuilder<Team> builder)
        {
            builder.ToTable("Team");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Description).IsRequired().IsUnicode(false).HasMaxLength(255);
            builder.Property(x => x.ResponsibleId).IsRequired();
            builder.Property(x => x.CreationDate).IsRequired();
			builder.Property(x => x.ShiftId).IsRequired();
			builder.HasOne(x => x.Shift);
			builder.Navigation(x => x.Shift).IsRequired();

            builder.Property(x => x.SiteId).IsRequired();
            builder.Property(x => x.CompanyId).IsRequired(false);
            builder.Property(x => x.BuildingId).IsRequired(false);
            builder.Property(x => x.CustomerId).IsRequired(false);
            builder.Property(x => x.DivisionId).IsRequired(false);
            builder.Property(x => x.AreaId).IsRequired(false);

            builder.Property(x => x.IsActive).IsRequired();
            builder.HasMany(x => x.TeamProducts)
                .WithOne()
                .HasForeignKey(p => p.TeamId);
        }
    }
}