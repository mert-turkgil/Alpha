using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Alpha.Entity;

namespace Data.Configuration
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(m => m.CategoryId);
            builder.Property(m => m.Name)
                   .IsRequired()
                   .HasMaxLength(150);
            builder.Property(m => m.Url)
                   .IsRequired()
                   .HasMaxLength(150);
        }
    }
}
