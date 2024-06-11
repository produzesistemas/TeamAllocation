using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MFGAllocation.Mappings
{
    public class WorkstationMap: IEntityTypeConfiguration<Workstation>
    {
        public WorkstationMap() { }

        public void Configure(EntityTypeBuilder<Workstation> builder)
        {
            builder.ToTable("Workstation");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductId).IsRequired();

            builder.Property(x => x.LayoutId).IsRequired();

            builder.Property(x => x.SiteId).IsRequired();

            builder.Property(x => x.Name).IsRequired().IsUnicode(false).HasMaxLength(255);

            builder.Property(x => x.CriticalityLevel).IsRequired();

            builder.Property(x => x.AllowMultiHC);

            builder.Property(x => x.IsActive).IsRequired();

            builder
               .Ignore(x => x.Trainings)
               .Property<IEnumerable<Guid>>("Trainings")
               .HasArrayToJsonConversion()
               .HasColumnName("Trainings")
               .IsRequired();
        }
    }
}
