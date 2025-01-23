using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha.Entity;

namespace Alpha.Entity
{
    public class Category
    {
        #nullable disable
        public Category()
        {
            ProductCategories = new List<ProductCategory>();
            CategoryBlogs = new List<CategoryBlog>();
        }

        public int CategoryId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;

        public virtual ICollection<ProductCategory> ProductCategories { get; set; }
        public virtual ICollection<CategoryBlog> CategoryBlogs { get; set; }
    }
}