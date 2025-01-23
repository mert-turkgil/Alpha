using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha.Entity;

namespace Alpha.Models
{
    public class ProductDetailViewModel
    {
        #nullable disable
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Upper { get; set; }
        public string Lining { get; set; }
        public string Protection { get; set; }
        public string Midsole { get; set; }
        public string Insole { get; set; }
        public string Sole { get; set; }
        public string Model { get; set; }
        public string Standard { get; set; }
        public string Certificate { get; set; }
        public string Brand { get; set; }
        public string Size { get; set; }
        public int BodyNo { get; set; }
        public DateTime DateAdded { get; set; }

        // Product Images
        public List<ProductImage> ProductImages { get; set; } = new();

        // Product images Information
        public List<Image> Images { get; set; }

        // Related Blogs
        public List<Blog> RelatedBlogs { get; set; } = new();

        // Related Categories
        public string CategoryName { get; set; }
        public List<Category> RelatedCategories { get; set; } = new();

        // Recent Products
        public List<Product> RecentProducts { get; set; } = new();

        // Non-variable Text Labels for Localization
        public string DescriptionLabel { get; set; }
        public string UpperLabel { get; set; }
        public string LiningLabel { get; set; }
        public string ProtectionLabel { get; set; }
        public string MidsoleLabel { get; set; }
        public string InsoleLabel { get; set; }
        public string SoleLabel { get; set; }
        public string ModelLabel { get; set; }
        public string BrandLabel { get; set; }
        public string StandardLabel { get; set; }
        public string CertificateLabel { get; set; }
        public string SizeLabel { get; set; }
        public string CategoryLabel { get; set; }
        public string DateAddedLabel { get; set; }
        public string ImagesLabel { get; set; }
        public string BtnText { get; set; }
    }
}