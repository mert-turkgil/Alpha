using Alpha.Models;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Alpha.Services
{
    public class ResxResourceService : IResxResourceService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _resourcesPath;

        public ResxResourceService(IWebHostEnvironment env)
        {
            _env = env;
            _resourcesPath = Path.Combine(_env.ContentRootPath, "Resources");
        }

        public List<LocalizationModel> LoadAll(string culture)
        {
            var resxPath = GetResourcePath(culture);
            if (!File.Exists(resxPath))
                return new List<LocalizationModel>();

            try
            {
                using (var stream = new FileStream(resxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var doc = XDocument.Load(stream);
                    return doc.Root?
                        .Elements("data")
                        .Select(x => new LocalizationModel
                        {
                            Key = x.Attribute("name")?.Value ?? "",
                            Value = x.Element("value")?.Value ?? "",
                            Comment = x.Element("comment")?.Value ?? ""
                        })
                        .ToList() ?? new List<LocalizationModel>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResxResourceService] Error loading all resources: {ex.Message}");
                return new List<LocalizationModel>();
            }
        }

        public bool AddOrUpdate(string key, string value, string culture, string? comment = null)
        {
            var resxPath = GetResourcePath(culture);
            if (!File.Exists(resxPath))
                return false;

            try
            {
                XDocument doc;
                
                // Use FileShare.Read to allow other processes to read while we write
                using (var stream = new FileStream(resxPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    doc = XDocument.Load(stream);
                }

                var data = doc.Root?.Elements("data").FirstOrDefault(x => x.Attribute("name")?.Value == key);

                if (data == null)
                {
                    data = new XElement("data",
                                new XAttribute("name", key),
                                new XAttribute(XNamespace.Xml + "space", "preserve"),
                                new XElement("value", value ?? ""),
                                new XElement("comment", comment ?? "")
                            );
                    doc.Root?.Add(data);
                }
                else
                {
                    data.SetElementValue("value", value ?? "");
                    data.SetElementValue("comment", comment ?? "");
                }

                // Save with proper file handling
                using (var stream = new FileStream(resxPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    doc.Save(stream);
                }

                // Force file system to flush
                System.Threading.Thread.Sleep(10);
                
                Console.WriteLine($"[ResxResourceService] Updated key '{key}' in {culture}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResxResourceService] Error updating resource: {ex.Message}");
                return false;
            }
        }

        public bool Delete(string key, string culture)
        {
            var resxPath = GetResourcePath(culture);
            if (!File.Exists(resxPath))
                return false;

            try
            {
                XDocument doc;
                
                using (var stream = new FileStream(resxPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    doc = XDocument.Load(stream);
                }

                var data = doc.Root?.Elements("data").FirstOrDefault(x => x.Attribute("name")?.Value == key);

                if (data == null)
                    return false;

                data.Remove();
                
                using (var stream = new FileStream(resxPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    doc.Save(stream);
                }

                System.Threading.Thread.Sleep(10);
                
                Console.WriteLine($"[ResxResourceService] Deleted key '{key}' from {culture}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResxResourceService] Error deleting resource: {ex.Message}");
                return false;
            }
        }

        public string? Read(string key, string culture)
        {
            var resxPath = GetResourcePath(culture);
            if (!File.Exists(resxPath))
                return null;

            try
            {
                using (var stream = new FileStream(resxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var doc = XDocument.Load(stream);
                    var value = doc.Root?
                        .Elements("data")
                        .FirstOrDefault(x => x.Attribute("name")?.Value == key)?
                        .Element("value")?.Value;

                    return value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResxResourceService] Error reading resource: {ex.Message}");
                return null;
            }
        }

        public bool Exists(string key, string culture)
        {
            var resxPath = GetResourcePath(culture);
            if (!File.Exists(resxPath))
                return false;

            try
            {
                using (var stream = new FileStream(resxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var doc = XDocument.Load(stream);
                    return doc.Root?
                        .Elements("data")
                        .Any(x => x.Attribute("name")?.Value == key) ?? false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResxResourceService] Error checking existence: {ex.Message}");
                return false;
            }
        }

        public List<string> GetAvailableLanguages()
        {
            var resxFiles = Directory
                .GetFiles(_resourcesPath, "SharedResource.*.resx")
                .Select(Path.GetFileName)
                .Where(f => !string.IsNullOrWhiteSpace(f)) // Ensure not null or empty
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .Where(name => !string.IsNullOrWhiteSpace(name) && name!.StartsWith("SharedResource."))
                .Select(name => name!.Replace("SharedResource.", ""))
                .ToList();

            return resxFiles;
        }


        public List<string> ExtractImagePaths(string keyPrefix, string culture)
        {
            List<string> imagePaths = new();
            var resxPath = GetResourcePath(culture);
            if (!File.Exists(resxPath))
                return imagePaths;

            try
            {
                using (var stream = new FileStream(resxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var doc = XDocument.Load(stream);
                    var dataElements = doc.Root?
                        .Elements("data")
                        .Where(x => x.Attribute("name")?.Value.StartsWith(keyPrefix) == true);

                    if (dataElements != null)
                    {
                        foreach (var element in dataElements)
                        {
                            var content = element.Element("value")?.Value ?? "";
                            var matches = Regex.Matches(content, @"src=[""'](?<url>/blog/(img|gif)/.*?\.(jpg|jpeg|png|gif))[""']");
                            foreach (Match match in matches)
                            {
                                imagePaths.Add(match.Groups["url"].Value.TrimStart('/'));
                            }
                        }
                    }

                    return imagePaths;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResxResourceService] Error extracting image paths: {ex.Message}");
                return imagePaths;
            }
        }

        public string GetResourcePath(string culture)
        {
            var fileName = $"SharedResource.{culture}.resx";
            return Path.Combine(_resourcesPath, fileName);
        }
    }
}
