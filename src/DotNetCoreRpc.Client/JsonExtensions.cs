using System;
using Newtonsoft.Json;

namespace DotNetCoreRpc.Client
{
    public static class JsonExtensions
    {
        public static string ToJson<T>(this T data) where T:class,new()
        {
            return JsonConvert.SerializeObject(data);
        }

        public static T FromJson<T>(this string json) where T : class, new()
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static object FromJson(this string json,Type type)
        {
            return JsonConvert.DeserializeObject(json,type);
        }
    }
}
