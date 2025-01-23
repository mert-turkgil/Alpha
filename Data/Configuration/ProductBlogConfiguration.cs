using Alpha.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Configuration
{
    public class ProductBlogConfiguration : IEntityTypeConfiguration<ProductBlog>
    {
        public void Configure(EntityTypeBuilder<ProductBlog> builder)
        {
            builder.HasKey(c => new { c.ProductId, c.BlogId });

            // Product relationship
            builder.HasOne(pb => pb.Product)
                   .WithMany(p => p.ProductBlogs)
                   .HasForeignKey(pb => pb.ProductId)
                   .OnDelete(DeleteBehavior.NoAction); 

            // Blog relationship - specify the correct navigation property here
            builder.HasOne(pb => pb.Blog)
                   .WithMany(b => b.ProductBlogs) // Use the navigation property on Blog
                   .HasForeignKey(pb => pb.BlogId)
                   .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
