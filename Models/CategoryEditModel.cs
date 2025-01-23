using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Alpha.Models
{
    public class CategoryEditModel
    {
        public int CategoryId { get; set; }
        #nullable disable
        [Required(ErrorMessage = "Category name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "URL is required.")]
        [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "URL must be lowercase and contain only letters, numbers, and dashes.")]
        public string Url { get; set; }
    [Display(Name = "Upload Image")]
    public IFormFile ImageFile { get; set; } // Image Upload Support

    public string ImageUrl { get; set; }
    }

}