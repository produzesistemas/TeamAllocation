using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MFGAllocation.Mappings
{
    public class ProductionPlanMap : IEntityTypeConfiguration<ProductionPlan>
    {
        public void Configure(EntityTypeBuilder<ProductionPlan> builder)
        {
            builder.ToTable("ProductionPlan");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.AllocationPlanId).IsRequired();
            builder.HasOne(x => x.AllocationPlan);
            builder.Navigation(x => x.AllocationPlan);

            builder.Property(x => x.ProductId).IsRequired();
            builder.HasOne(x => x.Product).WithMany().OnDelete(DeleteBehavior.NoAction);
            builder.Navigation(x => x.Product);

            builder.Property(x => x.ProductionLineId).IsRequired();
            builder.HasOne(x => x.ProductionLine).WithMany().OnDelete(DeleteBehavior.NoAction);
            builder.Navigation(x => x.ProductionLine);

            builder.Property(x => x.LayoutId).IsRequired();
            builder.HasOne(x => x.Layout);
            builder.Navigation(x => x.Layout);

            builder.Property(x => x.TeamId).IsRequired(false);
            builder.HasOne(x => x.Team).WithMany().OnDelete(DeleteBehavior.NoAction);
            builder.Navigation(x => x.Team);

            builder.Property(x => x.ShiftId).IsRequired(false);
            builder.HasOne(x => x.Shift).WithMany().OnDelete(DeleteBehavior.NoAction);
            builder.Navigation(x => x.Shift);

            builder.Property(x => x.RiseId).IsRequired(false);
            builder.HasOne(x => x.Rise).WithMany().OnDelete(DeleteBehavior.NoAction);

            builder.Property(x => x.Quantity).IsRequired();

            builder.Property(x => x.Velocity).IsRequired();

            builder.Property(x => x.Priority).IsRequired();
            builder.Property(x => x.Alias);
        }
    }
}
