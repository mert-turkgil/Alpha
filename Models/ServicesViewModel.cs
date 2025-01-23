using Alpha.Entity;

namespace Alpha.Models
{
    public class ServicesViewModel
    {
        #nullable disable
        public IEnumerable<Product> Products { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        // Localized Strings
        public string Services_Title { get; set; }
        public string Services_HeroDescription { get; set; }
        public string Services_CatalogFilterTitle { get; set; }
        public string Services_ViewCatalogButton { get; set; }
        public string Services_FilterProductsTitle { get; set; }
        public string Services_CategoryLabel { get; set; }
        public string Services_BrandLabel { get; set; }
        public string Services_BrandPlaceholder { get; set; }
        public string Services_SearchProductLabel { get; set; }
        public string Services_ApplyFiltersButton { get; set; }
        public string Services_OurProductsTitle { get; set; }
        public string Services_NoProductsMessage { get; set; }

        public string button { get; set; }
    }
}
