using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha.Entity;
using Alpha.Models;
using Alpha.Services;
using Data.Abstract;
using Microsoft.AspNetCore.Mvc;

namespace Alpha.ViewComponents
{
    public class ProductViewComponent : ViewComponent
    {
        private readonly IProductRepository _productRepository;
        private readonly LanguageService _localization;

        public ProductViewComponent(IProductRepository productRepository, LanguageService localization)
        {
            _productRepository = productRepository;
            _localization = localization;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // 1) Localized heading from your LanguageService
            var productHead = _localization.GetKey("HomeProductHead");

            // 2) Fetch all products
            var allProducts = await _productRepository.GetAllAsync();

            // 3) Determine "recent" or fallback products
            var (filteredProducts, statusMessage) = GetRecentProductsWithFallback(allProducts);

            // 4) Convert to a list of ProductCardViewModel with dynamic translations
            var culture = System.Globalization.CultureInfo.CurrentCulture.Name;

            var productCardList = filteredProducts
                .Select(p => new ProductCardViewModel
                {
                    ProductId = p.ProductId,
                    Name =  p.Name,
                    BodyNo = p.BodyNo,
                    Url = p.Url,
                    Upper = _localization.GetKey($"Product_{p.ProductId}_Upper_{culture}") ?? p.Upper,
                    Lining = _localization.GetKey($"Product_{p.ProductId}_Lining_{culture}") ?? p.Lining,
                    Protection = _localization.GetKey($"Product_{p.ProductId}_Protection_{culture}") ?? p.Protection,
                    Brand = p.Brand,
                    Standard = _localization.GetKey($"Product_{p.ProductId}_Standard_{culture}") ?? p.Standard,
                    Midsole = _localization.GetKey($"Product_{p.ProductId}_Midsole_{culture}") ?? p.Midsole,
                    Insole = _localization.GetKey($"Product_{p.ProductId}_Insole_{culture}") ?? p.Insole,
                    Certificate = p.Certificate,
                    Size = _localization.GetKey($"Product_{p.ProductId}_Size_{culture}") ?? p.Size,
                    Model = _localization.GetKey($"Product_{p.ProductId}_Model_{culture}") ?? p.Model,
                    Sole = _localization.GetKey($"Product_{p.ProductId}_Sole_{culture}") ?? p.Sole,
                    Description =_localization.GetKey($"Product_{p.ProductId}_Description_{culture}") ?? p.Description,
                    DateAdded = p.DateAdded,
                    CategoryId = p.CategoryId,
                    ProductImages = p.ProductImages?.ToList() ?? new List<ProductImage>(),
                    ProductCategories = p.ProductCategories?.ToList() ?? new List<ProductCategory>(),
                    ProductBlogs = p.ProductBlogs?.ToList() ?? new List<ProductBlog>(),
                })
                .ToList();

            // 5) Build the top-level view model
            var viewModel = new ProductIndexViewModel
            {
                Products = productCardList,
                StatusMessage = statusMessage,
                ProductHead = productHead,
                AvailableLanguages = new List<string> { "en", "fr", "de", "tr","ar" }
            };

            // 6) Return strongly-typed view
            return View(viewModel);
        }


        /// <summary>
        /// Determines "recent" products or falls back to the 4 most recent if none are found.
        /// Returns a tuple of (filtered product list, status message).
        /// </summary>
        private (List<Product> Filtered, string StatusMessage) GetRecentProductsWithFallback(List<Product> products)
        {
            var oneWeekAgo  = DateTime.Now.AddDays(-7);
            var oneMonthAgo = DateTime.Now.AddMonths(-1);
            var oneYearAgo  = DateTime.Now.AddYears(-1);

            string message = _localization.GetKey("Recently");

            // 1. Check last week
            var filteredProducts = products
                .Where(p => p.DateAdded >= oneWeekAgo)
                .ToList();

            if (filteredProducts.Any())
            {
                message = _localization.GetKey("LastWeek");
                return (filteredProducts, message);
            }

            // 2. Check last month
            filteredProducts = products
                .Where(p => p.DateAdded >= oneMonthAgo)
                .ToList();

            if (filteredProducts.Any())
            {
                message = _localization.GetKey("LastMonth");
                return (filteredProducts, message);
            }

            // 3. Check last year
            filteredProducts = products
                .Where(p => p.DateAdded >= oneYearAgo)
                .ToList();

            if (filteredProducts.Any())
            {
                message = _localization.GetKey("LastYear");
                return (filteredProducts, message);
            }

            // 4. Fallback: get 4 most recent
            filteredProducts = products
                .OrderByDescending(p => p.DateAdded)
                .Take(4)
                .ToList();

            if (filteredProducts.Any())
            {
                message = _localization.GetKey("FallbackProducts");
            }
            else
            {
                message = _localization.GetKey("NoProductsAvailable");
            }

            return (filteredProducts, message);
        }
    }
}
