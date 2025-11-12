using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace Alpha.Middleware
{
    public class CultureRedirectMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly string[] SupportedCultures = { "en", "tr", "de", "fr", "ar" };

        public CultureRedirectMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Check if the request is for the root path
            if (path == "/" || path == "")
            {
                // Check if user has a preference cookie
                var cultureCookie = context.Request.Cookies[".AspNetCore.Culture"];
                string detectedCulture = "en"; // Default fallback

                if (!string.IsNullOrEmpty(cultureCookie))
                {
                    // Parse culture from cookie (format: c=en-US|uic=en-US)
                    var cultureParts = cultureCookie.Split('|');
                    if (cultureParts.Length > 0)
                    {
                        var culturePart = cultureParts[0].Split('=');
                        if (culturePart.Length > 1)
                        {
                            var cultureCode = culturePart[1].Split('-')[0]; // Get just "en" from "en-US"
                            if (SupportedCultures.Contains(cultureCode))
                            {
                                detectedCulture = cultureCode;
                            }
                        }
                    }
                }
                else
                {
                    // Detect from Accept-Language header
                    var acceptLanguage = context.Request.Headers["Accept-Language"].ToString();
                    if (!string.IsNullOrEmpty(acceptLanguage))
                    {
                        var languages = acceptLanguage.Split(',')
                            .Select(lang => lang.Split(';')[0].Trim().ToLower())
                            .ToList();

                        foreach (var lang in languages)
                        {
                            // Try exact match first
                            if (SupportedCultures.Contains(lang))
                            {
                                detectedCulture = lang;
                                break;
                            }
                            // Try language prefix (e.g., "en-US" -> "en")
                            var langPrefix = lang.Split('-')[0];
                            if (SupportedCultures.Contains(langPrefix))
                            {
                                detectedCulture = langPrefix;
                                break;
                            }
                        }
                    }

                    // Geo-location based detection (optional - can use IP-based service)
                    // For now, we'll rely on Accept-Language header
                }

                // Redirect to the detected culture
                context.Response.Redirect($"/{detectedCulture}");
                return;
            }

            await _next(context);
        }
    }
}
