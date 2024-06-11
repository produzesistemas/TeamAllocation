using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MFGAllocation.Mappings
{
    public class ProductWorkstationMap : IEntityTypeConfiguration<ProductWorkstation>
    {
        public void Configure(EntityTypeBuilder<ProductWorkstation> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property<Guid>(x => x.Id).IsRequired();
            builder.Property<Guid>(x => x.ProductId).IsRequired();
            builder.Property<string>(x => x.Name).IsRequired();
        }
    }
}
