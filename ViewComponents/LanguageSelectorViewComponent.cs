using Microsoft.AspNetCore.Mvc;
using Alpha.Services;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebUI.ViewComponents
{
    public class LanguageSwitchModel
    {
        public string Culture { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class LanguageSelectorViewComponent : ViewComponent
    {
        private readonly string[] _supportedCultures = { "tr", "en", "de", "fr", "ar" };

        public IViewComponentResult Invoke()
        {
            var currentCulture = CultureInfo.CurrentCulture.Name;
            var currentShortCulture = currentCulture.Split('-')[0];
            
            // Get the current path
            var path = HttpContext.Request.Path.Value ?? "/";
            var pathSegments = path.Split('/').Where(s => !string.IsNullOrEmpty(s)).ToList();

            // Remove culture from path if present
            if (pathSegments.Count > 0 && _supportedCultures.Contains(pathSegments[0]))
            {
                pathSegments.RemoveAt(0);
            }

            var model = _supportedCultures.Select(culture =>
            {
                var displayName = culture switch
                {
                    "tr" => "Türkçe",
                    "en" => "English",
                    "de" => "Deutsch",
                    "fr" => "Français",
                    "ar" => "العربية",
                    _ => culture
                };

                // Build URL with new culture
                var newPath = $"/{culture}";
                if (pathSegments.Count > 0)
                {
                    // Translate the route segments for the new culture
                    var translatedSegments = new List<string>();
                    foreach (var segment in pathSegments)
                    {
                        // Get the route key from current culture, then translate to new culture
                        var routeKey = RouteTranslationService.GetRouteKey(segment, currentShortCulture);
                        var translatedRoute = RouteTranslationService.GetRoute(routeKey, culture);
                        translatedSegments.Add(translatedRoute);
                    }
                    newPath += "/" + string.Join("/", translatedSegments);
                }

                return new LanguageSwitchModel
                {
                    Culture = culture,
                    DisplayName = displayName,
                    Url = newPath,
                    IsSelected = culture == currentShortCulture
                };
            }).ToList();

            return View(model);
        }
    }
}
