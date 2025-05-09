using System.Globalization;
using System.Xml.Linq;
using Alpha.Models;
using System.Xml;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Alpha.Services
{
    public class BlogResxService : IBlogResxService
    {
        private readonly string _basePath = Path.Combine(Directory.GetCurrentDirectory(), "BlogResources");

        private string GetResxFilePath(string culture)
        {
            return Path.Combine(_basePath, $"SharedResource.{culture}.resx");
        }

        public bool AddOrUpdate(string key, string value, string culture, string? comment = null)
        {
            string filePath = GetResxFilePath(culture);
            XDocument doc;

            if (!File.Exists(filePath))
            {
                // Create new resx document
                doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement("root"));
            }
            else
            {
                doc = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
            }

            var existing = doc.Root?.Elements("data")
                .FirstOrDefault(e => e.Attribute("name")?.Value == key);


            if (existing != null)
            {
                existing.Element("value")!.Value = value;
                if (comment != null)
                    existing.Element("comment")!.Value = comment;
            }
            else
            {
                
                var data = new XElement("data",
                    new XAttribute("name", key),
                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                    new XElement("value", value));

                if (comment != null)
                    data.Add(new XElement("comment", comment));
                if (doc.Root == null)
                {
                    var root = new XElement("root");
                    doc.Add(root);
                }
                doc.Root!.Add(data);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            doc.Save(filePath);
            Console.WriteLine($"[BLOG RESX] Updated: {key} for {culture}");
            return true;
        }
        public bool Delete(string key, string culture)
        {
            string filePath = GetResxFilePath(culture);
            if (!File.Exists(filePath)) return false;

            var doc = XDocument.Load(filePath);
            var element = doc.Root?.Elements("data")
                .FirstOrDefault(e => e.Attribute("name")?.Value == key);

            if (element != null)
            {
                element.Remove();
                doc.Save(filePath);
                Console.WriteLine($"[BLOG RESX] Deleted: {key} for {culture}");
                return true;
            }

            return false;
        }


        public string? Read(string key, string culture)
        {
            string filePath = GetResxFilePath(culture);
            if (!File.Exists(filePath)) return null;

            var doc = XDocument.Load(filePath);
            return doc.Root?.Elements("data")
                .FirstOrDefault(e => e.Attribute("name")?.Value == key)?
                .Element("value")?.Value;

        }

        public bool Exists(string key, string culture)
        {
            return Read(key, culture) != null;
        }

        public List<LocalizationModel> LoadAll(string culture)
        {
            string filePath = GetResxFilePath(culture);
            var result = new List<LocalizationModel>();

            if (!File.Exists(filePath)) return result;

            var doc = XDocument.Load(filePath);
            foreach (var data in doc.Root?.Elements("data") ?? Enumerable.Empty<XElement>())
            {
                result.Add(new LocalizationModel
                {
                    Key = data.Attribute("name")?.Value ?? "",
                    Value = data.Element("value")?.Value ?? "",
                    Comment = data.Element("comment")?.Value ?? "",
                    Culture = culture
                });
            }

            return result;
        }

        public List<string> ExtractImagePaths(string keyPrefix, string culture)
        {
            return LoadAll(culture)
                .Where(l => l.Key.StartsWith(keyPrefix) && l.Value.Contains("<img"))
                .SelectMany(l => ExtractImageSrcFromHtml(l.Value))
                .Distinct()
                .ToList();
        }

        private IEnumerable<string> ExtractImageSrcFromHtml(string html)
        {
            var list = new List<string>();
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            foreach (var img in doc.DocumentNode.SelectNodes("//img[@src]")?? Enumerable.Empty<HtmlAgilityPack.HtmlNode>())
            {
                var src = img.GetAttributeValue("src", "");
                if (!string.IsNullOrWhiteSpace(src))
                    list.Add(src);
            }

            return list;
        }
    }
}
