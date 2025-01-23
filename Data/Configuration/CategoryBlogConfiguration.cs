using System;
using Alpha.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Data.Configuration
{
    public class CategoryBlogConfiguration : IEntityTypeConfiguration<CategoryBlog>
    {
        public void Configure(EntityTypeBuilder<CategoryBlog> builder)
        {
            // Define composite primary key
            builder.HasKey(c => new { c.CategoryId, c.BlogId });

            // Configure relationship with Category
            builder.HasOne(c => c.Category)
                   .WithMany(c => c.CategoryBlogs)
                   .HasForeignKey(c => c.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

            // Configure relationship with Blog
            builder.HasOne(c => c.Blog)
                   .WithMany(b => b.CategoryBlogs)
                   .HasForeignKey(c => c.BlogId)
                   .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
        }
    }
}
