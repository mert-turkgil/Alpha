using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha.Entity;

namespace Alpha.Entity
{
    public class CategoryBlog
    {
        #nullable disable
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }

        public int BlogId { get; set; }
        public virtual Blog Blog { get; set; }
    }
}