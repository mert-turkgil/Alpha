using System.Collections.Generic;
using Alpha.Entity;

namespace Alpha.Models
{
    public class BlogDetailsViewModel
    {
        #nullable disable
        // The main Blog record
        public Blog Blog { get; set; }

        // Non-variable localized strings
        public string BlogDetail_PublishedOn { get; set; }
        public string BlogDetail_ByAuthor { get; set; }
        public string BlogDetail_RecentPosts { get; set; }
        public string BlogDetail_CategoriesTitle { get; set; }
        public string BlogDetail_NextPost { get; set; }
        public string BlogDetail_PreviousPost { get; set; }
        public string BlogDetail_RelatedCategories { get; set; }
        public string BlogDetail_RelatedProducts { get; set; }
        public string BlogDetail_EmptyVideoMsg { get; set; }
        public string BlogDetail_EmptyMapMsg { get; set; }
        public string BlogDetail_EmptyCategory { get; set; }

        public string ViewButton { get; set; }

        // Additional data to display
        public IEnumerable<Category> RelatedCategories { get; set; }
        public IEnumerable<Product> RelatedProducts { get; set; }
    }
}
