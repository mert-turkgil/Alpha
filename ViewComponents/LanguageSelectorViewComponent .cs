using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Alpha.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebUI.ViewComponents
{
    public class LanguageSelectorViewComponent : ViewComponent
    {
        private readonly string[] _supportedCultures = { "tr", "en", "de", "fr", "ar" };
        
        public IViewComponentResult Invoke()
        {
            var cultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();

            // Null check for cultureFeature
            if (cultureFeature?.RequestCulture?.Culture == null)
            {
                throw new InvalidOperationException("RequestCulture feature is not available.");
            }

            var currentCulture = cultureFeature.RequestCulture.Culture.TwoLetterISOLanguageName.ToLower();
            var currentPath = HttpContext.Request.Path.Value ?? "";
            var currentAction = HttpContext.GetRouteData().Values["action"]?.ToString()?.ToLower();
            var currentController = HttpContext.GetRouteData().Values["controller"]?.ToString()?.ToLower();
            
            // Build localized URLs for each culture
            var cultures = new List<LanguageSwitchModel>();

            foreach (var culture in _supportedCultures)
            {
                var cultureName = culture switch
                {
                    "tr" => "Türkçe",
                    "en" => "English",
                    "de" => "Deutsch",
                    "fr" => "Français",
                    "ar" => "العربية",
                    _ => culture.ToUpper()
                };

                var localizedUrl = BuildLocalizedUrl(currentPath, currentAction, currentController, currentCulture, culture);

                cultures.Add(new LanguageSwitchModel
                {
                    Culture = culture,
                    DisplayName = cultureName,
                    Url = localizedUrl,
                    IsSelected = culture == currentCulture
                });
            }

            return View(cultures);
        }

        private string BuildLocalizedUrl(string currentPath, string? action, string? controller, string fromCulture, string toCulture)
        {
            // Parse the current path
            var segments = currentPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (segments.Length == 0)
            {
                return $"/{toCulture}";
            }

            // First segment should be culture
            if (segments.Length >= 1 && _supportedCultures.Contains(segments[0]))
            {
                segments[0] = toCulture; // Replace culture
            }

            // Check if the second segment is a translatable route
            if (segments.Length >= 2)
            {
                var routeSegment = segments[1];
                
                // Try to find the route key from the current culture's route
                var routeKey = RouteTranslationService.GetRouteKey(routeSegment, fromCulture);
                
                // Get the translated route for the target culture
                var translatedRoute = RouteTranslationService.GetRoute(routeKey, toCulture);
                segments[1] = translatedRoute;
            }

            return "/" + string.Join("/", segments);
        }
    }

    public class LanguageSwitchModel
    {
        public string Culture { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Url { get; set; } = "";
        public bool IsSelected { get; set; }
    }
}
