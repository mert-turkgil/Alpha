using Alpha.Data;
using Alpha.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using System.Text;
using Data.Concrete.EfCore;

namespace Alpha.Controllers
{
    [Route("sitemap.xml")]
    public class SitemapController : Controller
    {
        private readonly ShopContext _context;
        private readonly string[] _cultures = new[] { "tr", "en", "de", "fr", "ar" };

        public SitemapController(ShopContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var products = await _context.Products
                .Include(p => p.ProductImages)
                    .ThenInclude(pi => pi.Image)
                .AsNoTracking()
                .ToListAsync();
                
            var blogs = await _context.Blogs.AsNoTracking().ToListAsync();
            var categories = await _context.Categories.AsNoTracking().ToListAsync();

            XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace xhtml = "http://www.w3.org/1999/xhtml";
            XNamespace image = "http://www.google.com/schemas/sitemap-image/1.1";

            var urlset = new XElement(xmlns + "urlset",
                new XAttribute(XNamespace.Xmlns + "xhtml", xhtml),
                new XAttribute(XNamespace.Xmlns + "image", image));

            // Home pages
            foreach (var culture in _cultures)
            {
                urlset.Add(BuildUrlWithHreflang($"{baseUrl}/{culture}", "home", baseUrl, culture));
            }

            // Static pages with translated routes
            var staticPages = new[] { "services", "about", "contact", "privacy" };
            foreach (var page in staticPages)
            {
                foreach (var culture in _cultures)
                {
                    var translatedRoute = RouteTranslationService.GetRoute(page, culture);
                    var url = $"{baseUrl}/{culture}/{translatedRoute}";
                    urlset.Add(BuildUrlWithHreflang(url, page, baseUrl, culture));
                }
            }

            // Blog list pages
            foreach (var culture in _cultures)
            {
                var url = $"{baseUrl}/{culture}/blog";
                urlset.Add(BuildUrlWithHreflang(url, "blog", baseUrl, culture));
            }

            // Products with images for rich snippets
            foreach (var p in products)
            {
                foreach (var culture in _cultures)
                {
                    var productRoute = RouteTranslationService.GetRoute("product", culture);
                    var url = $"{baseUrl}/{culture}/{productRoute}/{p.ProductId}/{p.Url}";
                    
                    // Get product image for rich snippet
                    var imageUrl = p.ProductImages?.FirstOrDefault()?.Image?.ImageUrl;
                    var fullImageUrl = !string.IsNullOrEmpty(imageUrl) 
                        ? $"{baseUrl}{imageUrl}" 
                        : null;
                    
                    urlset.Add(BuildProductUrl(url, "product", baseUrl, culture, p, fullImageUrl));
                }
            }

            // Blogs
            foreach (var b in blogs)
            {
                foreach (var culture in _cultures)
                {
                    var url = $"{baseUrl}/{culture}/blog/{b.BlogId}/{b.Url}";
                    urlset.Add(BuildUrlWithHreflang(url, "blog-detail", baseUrl, culture, blogId: b.BlogId));
                }
            }

            var sitemap = new XDocument(urlset);
            return Content(sitemap.ToString(), "application/xml", Encoding.UTF8);
        }

        private XElement BuildUrlWithHreflang(string loc, string pageType, string baseUrl, string currentCulture, int? blogId = null)
        {
            XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace xhtml = "http://www.w3.org/1999/xhtml";

            var urlElement = new XElement(xmlns + "url",
                new XElement(xmlns + "loc", loc),
                new XElement(xmlns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                new XElement(xmlns + "changefreq", "weekly"),
                new XElement(xmlns + "priority", pageType == "home" ? "1.0" : "0.8")
            );

            // Add hreflang for all cultures
            foreach (var culture in _cultures)
            {
                string href;
                switch (pageType)
                {
                    case "home":
                        href = $"{baseUrl}/{culture}";
                        break;
                    case "services":
                    case "about":
                    case "contact":
                    case "privacy":
                        var translatedRoute = RouteTranslationService.GetRoute(pageType, culture);
                        href = $"{baseUrl}/{culture}/{translatedRoute}";
                        break;
                    case "blog":
                        href = $"{baseUrl}/{culture}/blog";
                        break;
                    default:
                        href = $"{baseUrl}/{culture}";
                        break;
                }

                urlElement.Add(
                    new XElement(xhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", culture),
                        new XAttribute("href", href))
                );
            }

            // Add x-default hreflang
            urlElement.Add(
                new XElement(xhtml + "link",
                    new XAttribute("rel", "alternate"),
                    new XAttribute("hreflang", "x-default"),
                    new XAttribute("href", $"{baseUrl}/en"))
            );

            return urlElement;
        }

        private XElement BuildProductUrl(string loc, string pageType, string baseUrl, string currentCulture, Alpha.Entity.Product product, string? imageUrl)
        {
            XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace xhtml = "http://www.w3.org/1999/xhtml";
            XNamespace image = "http://www.google.com/schemas/sitemap-image/1.1";

            var urlElement = new XElement(xmlns + "url",
                new XElement(xmlns + "loc", loc),
                new XElement(xmlns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                new XElement(xmlns + "changefreq", "monthly"),
                new XElement(xmlns + "priority", "0.9")
            );

            // Add product image for Google rich snippets
            if (!string.IsNullOrEmpty(imageUrl))
            {
                urlElement.Add(
                    new XElement(image + "image",
                        new XElement(image + "loc", imageUrl),
                        new XElement(image + "title", product.Name ?? "Safety Footwear"),
                        new XElement(image + "caption", product.Description ?? $"Alpha {product.Name}")
                    )
                );
            }

            // Add hreflang for all cultures
            foreach (var culture in _cultures)
            {
                var productRoute = RouteTranslationService.GetRoute("product", culture);
                var href = $"{baseUrl}/{culture}/{productRoute}/{product.ProductId}/{product.Url}";
                
                urlElement.Add(
                    new XElement(xhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", culture),
                        new XAttribute("href", href))
                );
            }

            // Add x-default hreflang
            var defaultProductRoute = RouteTranslationService.GetRoute("product", "en");
            urlElement.Add(
                new XElement(xhtml + "link",
                    new XAttribute("rel", "alternate"),
                    new XAttribute("hreflang", "x-default"),
                    new XAttribute("href", $"{baseUrl}/en/{defaultProductRoute}/{product.ProductId}/{product.Url}"))
            );

            return urlElement;
        }
    }
}

