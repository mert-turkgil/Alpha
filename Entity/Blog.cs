using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Alpha.Entity
{
    public class Blog
    {
        #nullable disable
        // Parameterless constructor required for EF Core
        public Blog()
        {
            ProductBlogs = new HashSet<ProductBlog>();
            CategoryBlogs = new List<CategoryBlog>();
        }

        // Primary Key
        [Key]
        public int BlogId { get; set; }

        // Title with required and max length
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        // Content and additional fields
        public string Content { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;

        // Date and Author
        public DateTime Date { get; set; }

        [MaxLength(100)]
        public string Author { get; set; } = string.Empty;

        public string RawYT { get; set; } = string.Empty;
        public string RawMaps { get; set; } = string.Empty;

        // Relationships

        // One-to-Many relationship with Category
        public virtual ICollection<Category> Category { get; set; } // Virtual for lazy loading

        // Many-to-Many with Products
        public virtual ICollection<ProductBlog> ProductBlogs { get; set; } 

        // Many-to-Many with Categories
        public virtual ICollection<CategoryBlog> CategoryBlogs { get; set; } 
    }
}
