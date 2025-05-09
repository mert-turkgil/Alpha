using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace Alpha.Services
{
    public class LanguageService
    {
        private readonly AliveResourceService _aliveResourceService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LanguageService(AliveResourceService aliveResourceService, IHttpContextAccessor httpContextAccessor)
        {
            _aliveResourceService = aliveResourceService;
            _httpContextAccessor = httpContextAccessor;
        }

        public string GetKey(string key)
        {
            var culture = _httpContextAccessor.HttpContext?.Features
                .Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "tr-TR";

            return _aliveResourceService.GetResource(key, culture);
        }
    }
}
