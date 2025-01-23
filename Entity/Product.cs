using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Alpha.Entity
{
    public class Product
    {
        #nullable disable
        public Product()
        {
            ProductImages = new List<ProductImage>();
            ProductCategories = new List<ProductCategory>();
            ProductBlogs = new List<ProductBlog>();
        }

        [Key]
        public int ProductId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int BodyNo { get; set; }

        [Required, StringLength(200)]
        public string Url { get; set; } = string.Empty;

        [StringLength(100)]
        public string Upper { get; set; } = string.Empty;

        [StringLength(100)]
        public string Lining { get; set; } = string.Empty;

        [StringLength(100)]
        public string Protection { get; set; } = string.Empty;

        [StringLength(50)]
        public string Brand { get; set; } = string.Empty;

        [StringLength(50)]
        public string Standard { get; set; } = string.Empty;

        [StringLength(100)]
        public string Midsole { get; set; } = string.Empty;

        [StringLength(100)]
        public string Insole { get; set; } = string.Empty;

        [StringLength(200)]
        public string Certificate { get; set; } = string.Empty;

        [StringLength(10)]
        public string Size { get; set; } = string.Empty;

        [StringLength(50)]
        public string Model { get; set; } = string.Empty;

        [StringLength(50)]
        public string Sole { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty; // Added Description Field

        public DateTime DateAdded { get; set; }
        // Relationship

        public virtual ICollection<ProductImage> ProductImages { get; set; }
        public virtual ICollection<ProductCategory> ProductCategories { get; set; }
        public virtual ICollection<ProductBlog> ProductBlogs { get; set; }

        [ForeignKey("Category"), Required]
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }
    }
}
