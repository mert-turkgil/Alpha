using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Alpha.Models
{
    public class ImageCreateViewModel
    {
        #nullable disable
        [Required]
        public IFormFile File { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Text cannot be longer than 100 characters.")]
        public string Text { get; set; }

        public bool ViewPhone { get; set; }
    }
}
