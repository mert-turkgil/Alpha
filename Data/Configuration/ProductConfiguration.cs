using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Alpha.Entity;

namespace Data.Configuration
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.HasKey(m => m.ProductId);
            builder.Property(m => m.Name)
                   .IsRequired()
                   .HasMaxLength(150);
            builder.Property(m => m.DateAdded)
                   .HasDefaultValueSql("GETDATE()"); // SQL Server-specific default value
        }
    }
}
