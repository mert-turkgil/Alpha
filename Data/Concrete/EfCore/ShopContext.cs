using Microsoft.EntityFrameworkCore;
using Data.Configuration;
using Alpha.Entity;

namespace Data.Concrete.EfCore
{
    public class ShopContext : DbContext
    {
        // Default constructor
        public ShopContext()
        {
        }

        // Constructor to accept options
        public ShopContext(DbContextOptions<ShopContext> options) : base(options)
        {
        }

        // DbSet properties for the entities
        public required DbSet<Blog> Blogs { get; set; } = null!;
        public DbSet<Carousel> Carousels { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Image> Images { get; set; } = null!;
        public required  DbSet<Product> Products { get; set; } = null!;
        public DbSet<ProductBlog> ProductBlog { get; set; } = null!;
        public DbSet<ProductCategory> ProductCategory { get; set; } = null!;
        public DbSet<ProductImage> ProductImage { get; set; } = null!;
        public DbSet<CategoryBlog> CategoryBlog { get; set; } = null!;

        // Configuring the entity relationships and seed data
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Apply configurations for entities
            modelBuilder.ApplyConfiguration(new ProductConfiguration());
            modelBuilder.ApplyConfiguration(new CategoryConfiguration());
            modelBuilder.ApplyConfiguration(new BlogConfiguration());
            modelBuilder.ApplyConfiguration(new CarouselConfiguration());
            modelBuilder.ApplyConfiguration(new ImageConfiguration());
            modelBuilder.ApplyConfiguration(new ProductImageConfiguration());
            modelBuilder.ApplyConfiguration(new ProductCategoryConfiguration());
            modelBuilder.ApplyConfiguration(new ProductBlogConfiguration());
            modelBuilder.ApplyConfiguration(new CategoryBlogConfiguration());
            

            // Call custom seed extension method (if available)
            modelBuilder.Seed();

            base.OnModelCreating(modelBuilder);
        }

        // Configuring DbContext options
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Enable lazy loading proxies
            optionsBuilder.UseLazyLoadingProxies();

            // Call base configuration
            base.OnConfiguring(optionsBuilder);
        }
    }
}
