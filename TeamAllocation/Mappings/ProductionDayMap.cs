using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MFGAllocation.Mappings
{
    public class ProductionDayMap : IEntityTypeConfiguration<ProductionDay>
    {
        public void Configure(EntityTypeBuilder<ProductionDay> builder)
        {
            builder.ToTable("ProductionDay");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductionPlanId).IsRequired();
            builder.HasOne(x => x.ProductionPlan);
            builder.Navigation(x => x.ProductionPlan);

            builder.Property(x => x.ShiftId).IsRequired(false);
            builder.HasOne(x => x.Shift).WithMany().OnDelete(DeleteBehavior.NoAction);
            builder.Navigation(x => x.Shift);

            builder.Property(x => x.TeamId).IsRequired(false);
            builder.HasOne(x => x.Team).WithMany().OnDelete(DeleteBehavior.NoAction);
            builder.Navigation(x => x.Team);

            builder.Property(x => x.RiseId).IsRequired(false);
            builder.HasOne(x => x.Rise).WithMany().OnDelete(DeleteBehavior.NoAction);
            //builder.Navigation(x => x.Rise);

            builder.Property(x => x.Date);
            builder.Property(x => x.Quantity).IsRequired();
        }

        internal class GuidToNullConverter : ValueConverter<Guid?, Guid?>
        {
            public GuidToNullConverter(ConverterMappingHints? mappingHints = null)
                : base(x => x == Guid.Empty ? default : x, x => x, mappingHints)
            {
            }
        }
    }
}
