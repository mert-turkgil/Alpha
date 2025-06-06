using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha.Entity;

namespace Alpha.Entity
{
    public class ProductImage
    {
        #nullable disable
        public int ProductId { get; set; }
        public virtual Product Product { get; set; }

        public int ImageId { get; set; }
        public virtual Image Image { get; set; }
    }
}