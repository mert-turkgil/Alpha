using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Alpha.Entity;

namespace Data.Configuration
{
    public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
    {
        public void Configure(EntityTypeBuilder<ProductImage> builder)
        {
            builder.HasKey(c => new { c.ImageId, c.ProductId });

            builder.HasOne(pi => pi.Image)
                   .WithMany(i => i.ProductImages)
                   .HasForeignKey(pi => pi.ImageId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pi => pi.Product)
                   .WithMany(p => p.ProductImages)
                   .HasForeignKey(pi => pi.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
