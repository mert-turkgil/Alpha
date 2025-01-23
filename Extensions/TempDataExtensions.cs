using System;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;

namespace Alpha.Extensions
{
    public static class TempDataExtensions
    {
        public static void Put<T>(this ITempDataDictionary tempData, string key, T value) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Value cannot be null.");

            tempData[key] = JsonConvert.SerializeObject(value);
        }

        public static T? Get<T>(this ITempDataDictionary tempData, string key) where T : class
        {
            if (tempData.TryGetValue(key, out var o) && o is string serializedValue)
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(serializedValue);
                }
                catch (JsonSerializationException)
                {
                    return null;
                }
            }

            return null;
        }
    }
}
