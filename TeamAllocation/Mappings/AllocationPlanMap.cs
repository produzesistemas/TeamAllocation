using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MFGAllocation.Mappings
{
    internal class AllocationPlanMap : IEntityTypeConfiguration<AllocationPlan>
    {
        public void Configure(EntityTypeBuilder<AllocationPlan> builder)
        {
            builder.ToTable("AllocationPlan");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.IsActive).IsRequired();
            builder.Property(x => x.Number).IsRequired();
            builder.Property(x => x.StartDate).IsRequired();
            builder.Property(x => x.EndDate).IsRequired();
			builder.Property(x => x.CreationDate).IsRequired();

            builder.Property(x => x.SiteId).IsRequired();
            builder.Property(x => x.CompanyId).IsRequired(false);
            builder.Property(x => x.BuildingId).IsRequired(false);
            builder.Property(x => x.CustomerId).IsRequired(false);
            builder.Property(x => x.DivisionId).IsRequired(false);
            builder.Property(x => x.AreaId).IsRequired(false);

            builder.OwnsOne(x => x.Status, status => 
            { 
                status.Property(x => x.Name).IsRequired().IsUnicode(false).HasMaxLength(255);
            });
            builder.OwnsOne(x => x.Type, type => 
            { 
                type.Property(x => x.Name).IsRequired().IsUnicode(false).HasMaxLength(255); 
            });
            builder.OwnsOne(x => x.Engine, engine =>
            {
                engine.Property(x => x.Name).IsRequired().IsUnicode(false).HasMaxLength(255);
            });
        }
    }
}