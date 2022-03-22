using System;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DotNetCoreRpc.Core
{
    public static class JsonExtensions
    {
        public static string ToJson<T>(this T data) where T:class,new()
        {
            return JsonSerializer.Serialize(data, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        }

        public static T FromJson<T>(this string json) where T : class, new()
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        }

        public static object FromJson(this string json,Type type)
        {
            return JsonSerializer.Deserialize(json, type, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        }
    }
}
