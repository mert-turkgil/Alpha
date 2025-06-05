using Alpha.Data;
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
        private readonly string[] _cultures = new[] { "tr", "en", "de", "ar" };

        public SitemapController(ShopContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var products = await _context.Products.AsNoTracking().ToListAsync();
            var blogs = await _context.Blogs.AsNoTracking().ToListAsync();
            var categories = await _context.Categories.AsNoTracking().ToListAsync();

            XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace xhtml = "http://www.w3.org/1999/xhtml";

            var urlset = new XElement(xmlns + "urlset",
                new XAttribute(XNamespace.Xmlns + "xhtml", xhtml));

            foreach (var culture in _cultures)
            {
                // Static Pages
                urlset.Add(BuildUrlWithHreflang($"{baseUrl}/{culture}", culture, baseUrl));
                urlset.Add(BuildUrlWithHreflang($"{baseUrl}/{culture}/privacy", culture, baseUrl));
                urlset.Add(BuildUrlWithHreflang($"{baseUrl}/{culture}/contact", culture, baseUrl));
                urlset.Add(BuildUrlWithHreflang($"{baseUrl}/{culture}/about", culture, baseUrl));
                urlset.Add(BuildUrlWithHreflang($"{baseUrl}/{culture}/blog", culture, baseUrl));
                urlset.Add(BuildUrlWithHreflang($"{baseUrl}/{culture}/hizmetler", culture, baseUrl));

                // Products
                foreach (var p in products)
                {
                    var path = $"/{culture}/urun/{p.ProductId}/{p.Url}";
                    urlset.Add(BuildUrlWithHreflang($"{baseUrl}{path}", culture, baseUrl, $"/urun/{p.ProductId}/{p.Url}"));
                }

                // Blogs
                foreach (var b in blogs)
                {
                    var path = $"/{culture}/blog/{b.BlogId}/{b.Url}";
                    urlset.Add(BuildUrlWithHreflang($"{baseUrl}{path}", culture, baseUrl, $"/blog/{b.BlogId}/{b.Url}"));
                }

                // Categories
                foreach (var c in categories)
                {
                    var path = $"/{culture}/kategori/{c.CategoryId}/{c.Url}";
                    urlset.Add(BuildUrlWithHreflang($"{baseUrl}{path}", culture, baseUrl, $"/kategori/{c.CategoryId}/{c.Url}"));
                }
            }

            var sitemap = new XDocument(urlset);
            return Content(sitemap.ToString(), "application/xml", Encoding.UTF8);
        }

        private XElement BuildUrlWithHreflang(string loc, string currentCulture, string baseUrl, string relativePath = "")
        {
            XNamespace xmlns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XNamespace xhtml = "http://www.w3.org/1999/xhtml";

            var urlElement = new XElement(xmlns + "url",
                new XElement(xmlns + "loc", loc),
                new XElement(xmlns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                new XElement(xmlns + "changefreq", "monthly"),
                new XElement(xmlns + "priority", "0.7")
            );

            foreach (var culture in _cultures)
            {
                var href = $"{baseUrl}/{culture}{relativePath}";
                urlElement.Add(
                    new XElement(xhtml + "link",
                        new XAttribute("rel", "alternate"),
                        new XAttribute("hreflang", culture),
                        new XAttribute("href", href))
                );
            }

            return urlElement;
        }
    }
}
