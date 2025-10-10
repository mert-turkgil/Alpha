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

            try
            {
                if (!File.Exists(filePath))
                {
                    // Create new resx document
                    doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"),
                        new XElement("root"));
                }
                else
                {
                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        doc = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
                    }
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
                
                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    doc.Save(stream);
                }
                
                System.Threading.Thread.Sleep(10);
                
                Console.WriteLine($"[BLOG RESX] Updated: {key} for {culture}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BLOG RESX] Error updating resource: {ex.Message}");
                return false;
            }
        }
        public bool Delete(string key, string culture)
        {
            string filePath = GetResxFilePath(culture);
            if (!File.Exists(filePath)) return false;

            try
            {
                XDocument doc;
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    doc = XDocument.Load(stream);
                }

                var element = doc.Root?.Elements("data")
                    .FirstOrDefault(e => e.Attribute("name")?.Value == key);

                if (element != null)
                {
                    element.Remove();
                    
                    using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        doc.Save(stream);
                    }
                    
                    System.Threading.Thread.Sleep(10);
                    
                    Console.WriteLine($"[BLOG RESX] Deleted: {key} for {culture}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BLOG RESX] Error deleting resource: {ex.Message}");
                return false;
            }
        }


        public string? Read(string key, string culture)
        {
            string filePath = GetResxFilePath(culture);
            if (!File.Exists(filePath)) return null;

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var doc = XDocument.Load(stream);
                    return doc.Root?.Elements("data")
                        .FirstOrDefault(e => e.Attribute("name")?.Value == key)?
                        .Element("value")?.Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BLOG RESX] Error reading resource: {ex.Message}");
                return null;
            }
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

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var doc = XDocument.Load(stream);
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BLOG RESX] Error loading all resources: {ex.Message}");
                return result;
            }
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
