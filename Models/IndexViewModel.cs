using Alpha.Entity;

namespace Alpha.Models
{
    public class IndexViewModel
    {
        #nullable disable
        public List<Category> Categories { get; set; }
        public List<Carousel> Carousels { get; set; }

        public string HeroTitle { get; set; }

        public string HeroDescription { get; set; }
        public string HeroLink { get; set; }
        public string HeaderQuote { get; set; }
        public string BodyQuote1 { get; set; }
        public string BodyQuote2 { get; set; }
        public string BodyQuote3 { get; set; }
        public string Talker1 { get; set; }
        public string Talker2 { get; set; }
        public string Talker3 { get; set; }
        public string Company1 { get; set; }
        public string Company2 { get; set; }
        public string Company3 { get; set; }
    }
}