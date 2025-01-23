using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Alpha.Models
{
    // minimal ViewModel for demonstration
    public class CategoryGridViewModel
    {
        #nullable disable
        public List<Alpha.Entity.Category> Categories { get; set; }
        public int TotalItems { get; set; }
    }
}