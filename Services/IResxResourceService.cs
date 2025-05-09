using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha.Models;

namespace Alpha.Services
{
    public interface IResxResourceService
        {
            List<LocalizationModel> LoadAll(string culture);
            bool AddOrUpdate(string key, string value, string culture, string? comment = null);
            bool Delete(string key, string culture);
            string? Read(string key, string culture);
            bool Exists(string key, string culture);
            List<string> GetAvailableLanguages();
            List<string> ExtractImagePaths(string keyPrefix, string culture);
            string GetResourcePath(string culture);
        }
}