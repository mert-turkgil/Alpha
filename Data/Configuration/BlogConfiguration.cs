using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Alpha.Entity;

namespace Data.Configuration
{
    public class BlogConfiguration : IEntityTypeConfiguration<Blog>
    {
        public void Configure(EntityTypeBuilder<Blog> builder)
        {
            builder.HasKey(m => m.BlogId);
            builder.Property(m => m.Title)
                   .IsRequired()
                   .HasMaxLength(150);
            builder.Property(m => m.Date)
                   .HasDefaultValueSql("GETDATE()");
            builder.Property(m => m.Content)
                   .IsRequired(); // Assuming blogs must have content
        }
    }
}
