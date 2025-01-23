using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Alpha.Entity
{
    public class Image
    {
        public Image()
        {
            ProductImages = new List<ProductImage>();
        }

        [Key]
        public int ImageId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;

        public DateTime DateAdded { get; set; }
        public bool ViewPhone { get; set; }

        public virtual ICollection<ProductImage> ProductImages { get; set; }
    }
}
