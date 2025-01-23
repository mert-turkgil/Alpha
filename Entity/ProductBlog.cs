using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha.Entity;

namespace Alpha.Entity
{
    public class ProductBlog
    {
        #nullable disable
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        public int BlogId { get; set; }
        public virtual Blog Blog { get; set; }
    }
}