using System;
using System.Collections.Generic;
using Alpha.Entity;

namespace Alpha.Models
{
    public class ProductCardViewModel
    {
        #nullable disable
        // Basic Product Details
        public int ProductId { get; set; }
        public string Name { get; set; }
        public int BodyNo { get; set; }
        public string Url { get; set; }
        public string Upper { get; set; }
        public string Lining { get; set; }
        public string Protection { get; set; }
        public string Brand { get; set; }
        public string Standard { get; set; }
        public string Midsole { get; set; }
        public string Insole { get; set; }
        public string Certificate { get; set; }
        public string Size { get; set; }
        public string Model { get; set; }
        public string Sole { get; set; }
        public string Description { get; set; }
        public DateTime DateAdded { get; set; }
        public int CategoryId { get; set; }

        // For product images, categories, etc.
        public List<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public List<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
        public List<ProductBlog> ProductBlogs { get; set; } = new List<ProductBlog>();

        // Multi-language Translations (if needed)
        public string UpperFR { get; set; }
        public string LiningFR { get; set; }
        public string ProtectionFR { get; set; }
        public string MidsoleFR { get; set; }
        public string InsoleFR { get; set; }
        public string SoleFR { get; set; }

        public string UpperUS { get; set; }
        public string LiningUS { get; set; }
        public string ProtectionUS { get; set; }
        public string MidsoleUS { get; set; }
        public string InsoleUS { get; set; }
        public string SoleUS { get; set; }

        public string UpperDE { get; set; }
        public string LiningDE { get; set; }
        public string ProtectionDE { get; set; }
        public string MidsoleDE { get; set; }
        public string InsoleDE { get; set; }
        public string SoleDE { get; set; }

        public string UpperTR { get; set; }
        public string LiningTR { get; set; }
        public string ProtectionTR { get; set; }
        public string MidsoleTR { get; set; }
        public string InsoleTR { get; set; }
        public string SoleTR { get; set; }

        // In case you want to dynamically know which languages are available
        public List<string> AvailableLanguages { get; set; } = new List<string>();
    }
}
