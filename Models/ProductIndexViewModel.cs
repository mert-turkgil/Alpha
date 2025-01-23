using System;
using System.Collections.Generic;

namespace Alpha.Models
{
    public class ProductIndexViewModel
    {
        #nullable disable
        // This will hold all the product cards you want to display.
        public List<ProductCardViewModel> Products { get; set; } = new List<ProductCardViewModel>();

        // Message about recency or fallback
        public string StatusMessage { get; set; }

        // The heading/title of the product section
        public string ProductHead { get; set; }

        // If needed, you can also keep a date reference, 
        // though typically that might be inside each product:
        // public DateTime ReferenceDate { get; set; }

        // If you want to set which languages are available overall
        public List<string> AvailableLanguages { get; set; } = new List<string>();
    }
}
