using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha.Entity;

namespace Alpha.Models
{
    public class BlogFilterViewModel
    {
        #nullable disable
        public List<Blog> Blogs { get; set; }
        public IEnumerable<string> Categories { get; set; }
        public IEnumerable<string> Brands { get; set; }

        public string Category { get; set; }
        public string Brand { get; set; }
        public string SearchTerm { get; set; }

        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        // New localized fields (adjust as needed)
        public string BlogList_Title { get; set; }
        public string BlogList_Description { get; set; }
        public string BlogList_SearchLabel { get; set; }
        public string BlogList_CategoryLabel { get; set; }
        public string BlogList_AllCategories { get; set; }
        public string BlogList_BrandLabel { get; set; }
        public string BlogList_BrandPlaceholder { get; set; }
        public string BlogList_ApplyFiltersButton { get; set; }
        public string BlogList_NoPostsMessage { get; set; }
        public string BlogList_ReadMore { get; set; }
    }
}