using System.ComponentModel.DataAnnotations;
using Alpha.Services;
using Microsoft.AspNetCore.Http;

namespace Alpha.Models
{
    public class BlogEditModel
    {
        #nullable disable
        // Blog ID
        public int BlogId { get; set; }

        // Title and Content
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Content is required.")]
        public string Content { get; set; } // Main content with image embedded

        // URL (Slug)
        [Required(ErrorMessage = "URL is required.")]
        [MaxLength(255, ErrorMessage = "URL cannot exceed 255 characters.")]
        public string Url { get; set; }

        // Cover Image
        public IFormFile ImageFile { get; set; } // For uploading new image
        public string ExistingImage { get; set; } // For displaying current image

        // Author
        [Required(ErrorMessage = "Author is required.")]
        [MaxLength(100, ErrorMessage = "Author cannot exceed 100 characters.")]
        public string Author { get; set; }

        // Embed Fields
        public string RawYT { get; set; } = string.Empty;
        public string RawMaps { get; set; } = string.Empty;

        [RequiredNonEmptyList(ErrorMessage = "At least one category must be selected.")]
        public List<int> SelectedCategoryIds { get; set; }
        //Products
        public List<int> SelectedProductIds { get; set; } = new(); 
        // Translations
        [Required(ErrorMessage = "English title is required.")]
        public string TitleUS { get; set; }
        [Required(ErrorMessage = "English content is required.")]
        public string ContentUS { get; set; }

        [Required(ErrorMessage = "Turkish title is required.")]
        public string TitleTR { get; set; }
        [Required(ErrorMessage = "Turkish content is required.")]
        public string ContentTR { get; set; }

        [Required(ErrorMessage = "German title is required.")]
        public string TitleDE { get; set; }
        [Required(ErrorMessage = "German content is required.")]
        public string ContentDE { get; set; }

        [Required(ErrorMessage = "French title is required.")]
        public string TitleFR { get; set; }
        [Required(ErrorMessage = "French content is required.")]
        public string ContentFR { get; set; }
    }
}
