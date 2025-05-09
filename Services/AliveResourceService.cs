using System;
using System.Collections.Concurrent;
using System.IO;
using System.Xml.Linq;

namespace Alpha.Services
{
    public class AliveResourceService : IDisposable
    {
        private readonly ConcurrentDictionary<string, string> _resourceCache = new();
        private readonly string _resourcePath;
        private readonly FileSystemWatcher _watcher;

        public AliveResourceService(string resourcePath)
        {
            _resourcePath = resourcePath;

            _watcher = new FileSystemWatcher(resourcePath, "*.resx")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            _watcher.Changed += (sender, e) => ReloadResources();
            _watcher.Created += (sender, e) => ReloadResources();
            _watcher.Deleted += (sender, e) => ReloadResources();
            _watcher.Renamed += (sender, e) => ReloadResources();
        }

        public string GetResource(string key, string culture)
        {
            string cacheKey = $"{key}_{culture}";

            if (_resourceCache.TryGetValue(cacheKey, out var cachedValue))
                return cachedValue;

            var resxPath = Path.Combine(_resourcePath, $"SharedResource.{culture}.resx");
            if (File.Exists(resxPath))
            {
                var xDocument = XDocument.Load(resxPath);
                var dataElement = xDocument.Root?.Elements("data")
                    .FirstOrDefault(x => x.Attribute("name")?.Value == key);

                var value = dataElement?.Element("value")?.Value ?? $"[{key}]";
                _resourceCache[cacheKey] = value;

                return value;
            }

            return $"[{key}]";
        }

        public void ReloadResources()
        {
            Console.WriteLine($"Resources updated at {DateTime.Now}, clearing cache.");
            _resourceCache.Clear();
        }

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }
}
