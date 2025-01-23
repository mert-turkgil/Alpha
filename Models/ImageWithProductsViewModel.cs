namespace Alpha.Models
{
    public class ImageWithProductsViewModel
    {
        #nullable disable
        public int ImageId { get; set; }
        public string ImageUrl { get; set; }
        public string Text { get; set; }
        public DateTime DateAdded { get; set; }
        public bool ViewPhone { get; set; }

        // Associated product data
        public List<ProductViewModel> Products { get; set; } = new List<ProductViewModel>();
    }

    public class ProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
    }
}
