using System;
using System.Collections.Concurrent;
using System.IO;
using System.Xml.Linq;
using System.Threading;

namespace Alpha.Services
{
    public class AliveResourceService : IDisposable
    {
        private readonly ConcurrentDictionary<string, string> _resourceCache = new();
        private readonly ConcurrentDictionary<string, DateTime> _fileCache = new();
        private readonly string _resourcePath;
        private readonly FileSystemWatcher _watcher;
        private readonly Timer _debounceTimer;
        private readonly object _lock = new();
        private volatile bool _needsReload = false;

        public AliveResourceService(string resourcePath)
        {
            _resourcePath = resourcePath;
            Console.WriteLine($"[AliveResourceService] Initializing with path: {resourcePath}");

            _watcher = new FileSystemWatcher(resourcePath, "*.resx")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            // Debounce timer to avoid multiple rapid cache clears
            _debounceTimer = new Timer(OnDebounceTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.Deleted += OnFileChanged;
            _watcher.Renamed += OnFileChanged;

            Console.WriteLine($"[AliveResourceService] Watching: {resourcePath}");
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            lock (_lock)
            {
                _needsReload = true;
                // Reset the timer - will trigger after 500ms of no more changes
                _debounceTimer.Change(500, Timeout.Infinite);
            }
            Console.WriteLine($"[AliveResourceService] File changed detected: {e.Name}");
        }

        private void OnDebounceTimerElapsed(object? state)
        {
            if (_needsReload)
            {
                ReloadResources();
                _needsReload = false;
            }
        }

        public string GetResource(string key, string culture)
        {
            string cacheKey = $"{key}_{culture}";

            // Check if we need to invalidate cache based on file modification time
            var resxPath = Path.Combine(_resourcePath, $"SharedResource.{culture}.resx");
            
            Console.WriteLine($"[AliveResourceService] GetResource(key='{key}', culture='{culture}')");
            Console.WriteLine($"[AliveResourceService] Looking for file: {resxPath}");
            Console.WriteLine($"[AliveResourceService] File exists: {File.Exists(resxPath)}");
            
            if (File.Exists(resxPath))
            {
                var lastWriteTime = File.GetLastWriteTimeUtc(resxPath);
                
                // Check if file has been modified since we last cached it
                if (_fileCache.TryGetValue(culture, out var cachedTime))
                {
                    if (lastWriteTime > cachedTime)
                    {
                        // File was modified, clear cache for this culture
                        InvalidateCultureCache(culture);
                        _fileCache[culture] = lastWriteTime;
                        Console.WriteLine($"[AliveResourceService] Cache invalidated for culture: {culture}");
                    }
                }
                else
                {
                    _fileCache[culture] = lastWriteTime;
                }

                // Try to get from cache
                if (_resourceCache.TryGetValue(cacheKey, out var cachedValue))
                    return cachedValue;

                // Load from file with fresh read
                try
                {
                    using (var stream = new FileStream(resxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        var xDocument = XDocument.Load(stream);
                        var dataElement = xDocument.Root?.Elements("data")
                            .FirstOrDefault(x => x.Attribute("name")?.Value == key);

                        var value = dataElement?.Element("value")?.Value ?? $"[{key}]";
                        _resourceCache[cacheKey] = value;

                        return value;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AliveResourceService] Error reading resource: {ex.Message}");
                    return $"[{key}]";
                }
            }

            return $"[{key}]";
        }

        private void InvalidateCultureCache(string culture)
        {
            var keysToRemove = _resourceCache.Keys
                .Where(k => k.EndsWith($"_{culture}"))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _resourceCache.TryRemove(key, out _);
            }
        }

        public void ReloadResources()
        {
            Console.WriteLine($"[AliveResourceService] Resources updated at {DateTime.Now:yyyy-MM-dd HH:mm:ss}, clearing cache.");
            _resourceCache.Clear();
            _fileCache.Clear();
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _debounceTimer?.Dispose();
        }
    }
}
