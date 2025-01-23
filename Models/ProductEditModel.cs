using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Alpha.Entity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Alpha.Models
{
    public class ProductEditModel
    {
        #nullable disable

        // Basic Product Fields
        public int ProductId { get; set; }

        [Required(ErrorMessage = "The 'Name' field is required.")]
        [StringLength(100, ErrorMessage = "The 'Name' must be less than {1} characters.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "The 'Body Number' field is required.")]
        public int BodyNo { get; set; }

        [Required(ErrorMessage = "The 'URL' field is required.")]
        [StringLength(200, ErrorMessage = "The 'URL' must be less than {1} characters.")]
        public string Url { get; set; }

        [StringLength(100, ErrorMessage = "The 'Upper' must be less than {1} characters.")]
        public string Upper { get; set; }

        [StringLength(100, ErrorMessage = "The 'Protection' must be less than {1} characters.")]
        public string Protection { get; set; }

        [Required(ErrorMessage = "The 'Description' field is required.")]
        [StringLength(500, ErrorMessage = "The 'Description' must be less than {1} characters.")]
        public string Description { get; set; }

        [StringLength(100, ErrorMessage = "The 'Midsole' must be less than {1} characters.")]
        public string Midsole { get; set; }

        [StringLength(100, ErrorMessage = "The 'Insole' must be less than {1} characters.")]
        public string Insole { get; set; }

        [StringLength(100, ErrorMessage = "The 'Lining' must be less than {1} characters.")]
        public string Lining { get; set; }

        [StringLength(50, ErrorMessage = "The 'Brand' must be less than {1} characters.")]
        public string Brand { get; set; }

        [StringLength(50, ErrorMessage = "The 'Standard' must be less than {1} characters.")]
        public string Standard { get; set; }

        [StringLength(200, ErrorMessage = "The 'Certificate' must be less than {1} characters.")]
        public string Certificate { get; set; }

        [StringLength(10, ErrorMessage = "The 'Size' must be less than {1} characters.")]
        public string Size { get; set; }

        [StringLength(50, ErrorMessage = "The 'Model' must be less than {1} characters.")]
        public string Model { get; set; }

        [StringLength(50, ErrorMessage = "The 'Sole' must be less than {1} characters.")]
        public string Sole { get; set; }

        [Required(ErrorMessage = "The 'Category' field is required.")]
        public int CategoryId { get; set; }

        public List<int> BlogIds { get; set; } = new List<int>();
        public List<SelectListItem> AvailableBlogs { get; set; } = new();


        // Translations - Dynamic per language
        public Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();

        // List of selected image IDs associated with the product
         public List<Category> AvailableCategories { get; set; } = new List<Category>();
        public List<int> ImageIds { get; set; } = new List<int>();
            // Image and Category IDs

            public List<int> CategoryIds { get; set; } = new List<int>();

        // Available Images for Selection
        public List<Image> AvailableImages { get; set; } = new List<Image>();

        // Current Images Associated with the Product
        public List<Image> CurrentImages { get; set; } = new List<Image>();
    //translations
    [Required(ErrorMessage = "DescriptionFR is required.")]
    public string DescriptionFR { get; set; }
    [Required(ErrorMessage = "UpperFR is required.")]
    public string UpperFR { get; set; }

    [Required(ErrorMessage = "LiningFR is required.")]
    public string LiningFR { get; set; }

    [Required(ErrorMessage = "ProtectionFR is required.")]
    public string ProtectionFR { get; set; }

    [Required(ErrorMessage = "MidsoleFR is required.")]
    public string MidsoleFR { get; set; }

    [Required(ErrorMessage = "InsoleFR is required.")]
    public string InsoleFR { get; set; }

    [Required(ErrorMessage = "SoleFR is required.")]
    public string SoleFR { get; set; }
    //US
    [Required(ErrorMessage = "DescriptionUS is required.")]
    public string DescriptionUS { get; set; }

    [Required(ErrorMessage = "UpperUS is required.")]
    public string UpperUS { get; set; }

    [Required(ErrorMessage = "LiningUS is required.")]
    public string LiningUS { get; set; }

    [Required(ErrorMessage = "ProtectionUS is required.")]
    public string ProtectionUS { get; set; }

    [Required(ErrorMessage = "MidsoleUS is required.")]
    public string MidsoleUS { get; set; }

    [Required(ErrorMessage = "InsoleUS is required.")]
    public string InsoleUS { get; set; }
    //DE
    [Required(ErrorMessage = "DescriptionDE is required.")]
    public string DescriptionDE { get; set; }

    [Required(ErrorMessage = "SoleUS is required.")]
    public string SoleUS { get; set; }

    [Required(ErrorMessage = "UpperDE is required.")]
    public string UpperDE { get; set; }

    [Required(ErrorMessage = "LiningDE is required.")]
    public string LiningDE { get; set; }

    [Required(ErrorMessage = "ProtectionDE is required.")]
    public string ProtectionDE { get; set; }

    [Required(ErrorMessage = "MidsoleDE is required.")]
    public string MidsoleDE { get; set; }

    [Required(ErrorMessage = "InsoleDE is required.")]
    public string InsoleDE { get; set; }

    [Required(ErrorMessage = "SoleDE is required.")]
    public string SoleDE { get; set; }
    //TR
    [Required(ErrorMessage = "DescriptionTR is required.")]
    public string DescriptionTR { get; set; }

    [Required(ErrorMessage = "UpperTR is required.")]
    public string UpperTR { get; set; }

    [Required(ErrorMessage = "LiningTR is required.")]
    public string LiningTR { get; set; }

    [Required(ErrorMessage = "ProtectionTR is required.")]
    public string ProtectionTR { get; set; }

    [Required(ErrorMessage = "MidsoleTR is required.")]
    public string MidsoleTR { get; set; }

    [Required(ErrorMessage = "InsoleTR is required.")]
    public string InsoleTR { get; set; }

    [Required(ErrorMessage = "SoleTR is required.")]
    public string SoleTR { get; set; }
    }
}
