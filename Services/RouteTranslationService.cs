using System.Collections.Generic;

namespace Alpha.Services
{
    public class RouteTranslationService
    {
        // Dictionary to hold route translations for each culture
        private static readonly Dictionary<string, Dictionary<string, string>> _routeTranslations = new()
        {
            // English routes
            ["en"] = new Dictionary<string, string>
            {
                ["services"] = "services",
                ["about"] = "about",
                ["contact"] = "contact",
                ["privacy"] = "privacy",
                ["blog"] = "blog",
                ["product"] = "product"
            },
            // Turkish routes
            ["tr"] = new Dictionary<string, string>
            {
                ["services"] = "hizmetlerimiz",
                ["about"] = "hakkimizda",
                ["contact"] = "iletisim",
                ["privacy"] = "gizlilik",
                ["blog"] = "blog",
                ["product"] = "urun"
            },
            // German routes
            ["de"] = new Dictionary<string, string>
            {
                ["services"] = "dienstleistungen",
                ["about"] = "uber-uns",
                ["contact"] = "kontakt",
                ["privacy"] = "datenschutz",
                ["blog"] = "blog",
                ["product"] = "produkt"
            },
            // French routes
            ["fr"] = new Dictionary<string, string>
            {
                ["services"] = "services",
                ["about"] = "a-propos",
                ["contact"] = "contact",
                ["privacy"] = "confidentialite",
                ["blog"] = "blog",
                ["product"] = "produit"
            },
            // Arabic routes
            ["ar"] = new Dictionary<string, string>
            {
                ["services"] = "services",
                ["about"] = "about",
                ["contact"] = "contact",
                ["privacy"] = "privacy",
                ["blog"] = "blog",
                ["product"] = "product"
            }
        };

        // Reverse lookup dictionary for finding keys from values
        private static readonly Dictionary<string, Dictionary<string, string>> _reverseRouteLookup = new();

        static RouteTranslationService()
        {
            // Build reverse lookup dictionary
            foreach (var culture in _routeTranslations.Keys)
            {
                _reverseRouteLookup[culture] = new Dictionary<string, string>();
                foreach (var kvp in _routeTranslations[culture])
                {
                    _reverseRouteLookup[culture][kvp.Value] = kvp.Key;
                }
            }
        }

        /// <summary>
        /// Gets the translated route for a given key and culture
        /// </summary>
        public static string GetRoute(string key, string culture)
        {
            var cultureLower = culture.ToLower();
            if (_routeTranslations.ContainsKey(cultureLower) && 
                _routeTranslations[cultureLower].ContainsKey(key))
            {
                return _routeTranslations[cultureLower][key];
            }
            return key; // Fallback to key if translation not found
        }

        /// <summary>
        /// Gets the route key from a translated route value
        /// </summary>
        public static string GetRouteKey(string routeValue, string culture)
        {
            var cultureLower = culture.ToLower();
            if (_reverseRouteLookup.ContainsKey(cultureLower) && 
                _reverseRouteLookup[cultureLower].ContainsKey(routeValue))
            {
                return _reverseRouteLookup[cultureLower][routeValue];
            }
            return routeValue; // Fallback to value if key not found
        }

        /// <summary>
        /// Gets all supported cultures
        /// </summary>
        public static IEnumerable<string> GetSupportedCultures()
        {
            return _routeTranslations.Keys;
        }

        /// <summary>
        /// Checks if a route exists for a specific culture
        /// </summary>
        public static bool RouteExists(string routeValue, string culture)
        {
            var cultureLower = culture.ToLower();
            return _reverseRouteLookup.ContainsKey(cultureLower) && 
                   _reverseRouteLookup[cultureLower].ContainsKey(routeValue);
        }
    }
}
