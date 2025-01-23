using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Alpha.Models
{
    public class ImageEditModel
    {
       #nullable disable
        public int ImageId { get; set; }

        
        [StringLength(255)]
        public string ImageUrl { get; set; }

        [StringLength(500)]
        public string Text { get; set; }

        
        public DateTime DateAdded { get; set; }

        
        public bool ViewPhone { get; set; }
        
        
    }
}