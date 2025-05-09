using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Xml.Linq;
using System.Globalization;

namespace Alpha.Controllers
{
    public class SitemapController : Controller
    {
        [HttpGet("sitemap.xml")]
        public IActionResult Index()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var cultures = new[] { "tr", "en", "de", "fr", "ar" };

            var urls = new[]
            {
                "Index",
                "About",
                "Contact",
                "Services",
                "Blog"
            };

            var urlset = new XElement("urlset",
                new XAttribute("xmlns", "http://www.sitemaps.org/schemas/sitemap/0.9"),
                new XAttribute(XNamespace.Xmlns + "xhtml", "http://www.w3.org/1999/xhtml")
            );

            foreach (var action in urls)
            {
                foreach (var culture in cultures)
                {
                    var loc = $"{baseUrl}/{culture.ToLower()}/{action.ToLower()}";

                    var url = new XElement("url",
                        new XElement("loc", loc),
                        new XElement("changefreq", "weekly"),
                        new XElement("priority", action == "Index" ? "1.0" : "0.8")
                    );

                    foreach (var altCulture in cultures)
                    {
                        url.Add(new XElement(XName.Get("link", "http://www.w3.org/1999/xhtml"),
                            new XAttribute("rel", "alternate"),
                            new XAttribute("hreflang", altCulture),
                            new XAttribute("href", $"{baseUrl}/{altCulture}/{action.ToLower()}")
                        ));
                    }

                    urlset.Add(url);
                }
            }

            var xml = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), urlset);
            var xmlString = xml.ToString(SaveOptions.DisableFormatting);
            return Content(xmlString, "application/xml", Encoding.UTF8);
        }
    }
}
