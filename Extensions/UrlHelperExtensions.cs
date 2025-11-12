using Microsoft.AspNetCore.Mvc;
using Alpha.Services;
using System.Globalization;

namespace Alpha.Extensions
{
    public static class UrlHelperExtensions
    {
        /// <summary>
        /// Generates a localized URL based on the current culture
        /// </summary>
        public static string LocalizedAction(this IUrlHelper urlHelper, string action, string controller = "Home", object? routeValues = null)
        {
            var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
            
            // Map action to route key
            var routeKey = action.ToLower() switch
            {
                "services" => "services",
                "about" => "about",
                "contact" => "contact",
                "privacy" => "privacy",
                "productdetail" => "product",
                "blog" => "blog",
                "blogdetails" => "blog",
                "index" => "",
                _ => action.ToLower()
            };

            // Get translated route
            var translatedRoute = string.IsNullOrEmpty(routeKey) 
                ? "" 
                : RouteTranslationService.GetRoute(routeKey, culture);

            // Build URL
            var url = $"/{culture}";
            
            if (!string.IsNullOrEmpty(translatedRoute))
            {
                url += $"/{translatedRoute}";
            }

            // Add route values (for IDs, slugs, etc.)
            if (routeValues != null)
            {
                var props = routeValues.GetType().GetProperties();
                foreach (var prop in props)
                {
                    var value = prop.GetValue(routeValues)?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        url += $"/{value}";
                    }
                }
            }

            return url;
        }
    }
}
