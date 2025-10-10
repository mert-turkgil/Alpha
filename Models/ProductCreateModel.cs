using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Alpha.Entity;

namespace Alpha.Models
{
    public class ProductCreateModel
    {
        #nullable disable
        
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


        // Translations per language (optional - will fall back to main field values if not provided)
    public List<string> AvailableLanguages { get; set; } = new();
    
    //FR - French translations
    public string UpperFR { get; set; }
    public string DescriptionFR { get; set; }
    public string LiningFR { get; set; }
    public string ProtectionFR { get; set; }
    public string MidsoleFR { get; set; }
    public string InsoleFR { get; set; }
    public string SoleFR { get; set; }
    
    //US - English translations
    public string DescriptionUS { get; set; }
    public string UpperUS { get; set; }
    public string LiningUS { get; set; }
    public string ProtectionUS { get; set; }
    public string MidsoleUS { get; set; }
    public string InsoleUS { get; set; }
    public string SoleUS { get; set; }
    
    //DE - German translations
    public string DescriptionDE { get; set; }
    public string UpperDE { get; set; }
    public string LiningDE { get; set; }
    public string ProtectionDE { get; set; }
    public string MidsoleDE { get; set; }
    public string InsoleDE { get; set; }
    public string SoleDE { get; set; }
    
    //TR - Turkish translations
    public string DescriptionTR { get; set; }
    public string UpperTR { get; set; }
    public string LiningTR { get; set; }
    public string ProtectionTR { get; set; }
    public string MidsoleTR { get; set; }
    public string InsoleTR { get; set; }
    public string SoleTR { get; set; }
    
    //AR - Arabic translations
    public string DescriptionAR { get; set; }
    public string UpperAR { get; set; }
    public string LiningAR { get; set; }
    public string ProtectionAR { get; set; }
    public string MidsoleAR { get; set; }
    public string InsoleAR { get; set; }
    public string SoleAR { get; set; }
    }
}
