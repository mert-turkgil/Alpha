using System.Collections.Generic;

namespace Alpha
{
    /// <summary>
    /// Centralized configuration for supported cultures/languages.
    /// Modify this class to add or remove supported languages across the entire application.
    /// </summary>
    public static class CultureConfig
    {
        /// <summary>
        /// Supported cultures with their two-letter ISO language codes.
        /// Key: Full culture code (e.g., "en-US")
        /// Value: Two-letter ISO code (e.g., "en")
        /// </summary>
        public static readonly Dictionary<string, string> SupportedCultures = new()
        {
            { "en-US", "en" },
            { "tr-TR", "tr" },
            { "de-DE", "de" },
            { "fr-FR", "fr" },
            { "ar-SA", "ar" }
        };

        /// <summary>
        /// Gets all full culture codes (e.g., "en-US", "tr-TR")
        /// </summary>
        public static IEnumerable<string> GetCultureCodes() => SupportedCultures.Keys;

        /// <summary>
        /// Gets all two-letter language codes (e.g., "en", "tr")
        /// </summary>
        public static IEnumerable<string> GetLanguageCodes() => SupportedCultures.Values;

        /// <summary>
        /// Gets the two-letter code for a given culture.
        /// Example: GetLanguageCode("en-US") returns "en"
        /// </summary>
        public static string GetLanguageCode(string culture)
        {
            return SupportedCultures.TryGetValue(culture, out var langCode) 
                ? langCode 
                : "en"; // Default fallback
        }

        /// <summary>
        /// Checks if a culture is supported.
        /// </summary>
        public static bool IsCultureSupported(string culture)
        {
            return SupportedCultures.ContainsKey(culture);
        }

        /// <summary>
        /// Default culture when none is specified.
        /// </summary>
        public const string DefaultCulture = "tr-TR";

        /// <summary>
        /// Default language code when none is specified.
        /// </summary>
        public const string DefaultLanguage = "tr";
    }

    /// <summary>
    /// INSTRUCTIONS FOR ADDING A NEW LANGUAGE (e.g., Spanish)
    /// 
    /// 1. Add to SupportedCultures dictionary:
    ///    { "es-ES", "es" }
    /// 
    /// 2. Update Program.cs:
    ///    - RequestLocalizationOptions now uses CultureConfig.GetCultureCodes()
    /// 
    /// 3. Create resource files:
    ///    - Resources/SharedResource.es-ES.resx
    ///    - BlogResources/SharedResource.es-ES.resx
    /// 
    /// 4. Create email template:
    ///    - EmailTemplates/UserNotification_es.html
    ///    - EmailTemplates/UserConfirm_es.html (if used)
    /// 
    /// 5. Add to routing in Program.cs (if not using dynamic routing)
    /// 
    /// 6. Test:
    ///    - Visit https://yoursite.com/es/
    ///    - Create blog in Spanish via admin panel
    ///    - Submit contact form and check email in Spanish
    /// 
    /// That's it! AliveResourceService will automatically detect the new .resx files.
    /// </summary>
}
