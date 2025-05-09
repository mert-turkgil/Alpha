using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alpha.Models;

namespace Alpha.Services
{
    public interface IBlogResxService
    {
        bool AddOrUpdate(string key, string value, string culture, string? comment = null);
        string? Read(string key, string culture);
        bool Exists(string key, string culture);
        List<LocalizationModel> LoadAll(string culture);
        List<string> ExtractImagePaths(string keyPrefix, string culture);
        bool Delete(string key, string culture);
    }
}