using Alpha.Entity;
using Microsoft.AspNetCore.Identity;
using Alpha.Identity;

namespace Alpha.Models
{
    public class AdminPageModel
    {
        #nullable disable
        public IEnumerable<Category> Categories { get; set; }

        public List<Image> Images {get;set;}

        public List<Product> Products { get; set; }

        public List<Category> CategoriesList { get; set; }

        public List<Blog> Blog{get;set;}

        public List<Carousel> Carousel{get;set;}

        public IEnumerable<User> Users { get; set; }
        public IEnumerable<IdentityRole> Roles { get; set; }

    }
}