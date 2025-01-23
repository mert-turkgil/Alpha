using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Alpha.Entity;

namespace Data.Configuration
{
    public class ImageConfiguration : IEntityTypeConfiguration<Image>
    {
        public void Configure(EntityTypeBuilder<Image> builder)
        {
            builder.HasKey(m => m.ImageId);
            builder.Property(m => m.ImageUrl)
                   .IsRequired()
                   .HasMaxLength(255); // Optional: Define max length for the URL
            builder.Property(m => m.DateAdded)
                   .HasDefaultValueSql("GETDATE()");
        }
    }
}
