using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetCoreRpc.Core
{
    public static class JsonExtensions
    {
        private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions 
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        public static string ToJson<T>(this T data, JsonSerializerOptions options = default) where T:class,new()
        {
            return JsonSerializer.Serialize(data, options ?? serializerOptions);
        }

        public static T FromJson<T>(this string json, JsonSerializerOptions options = default) where T : class, new()
        {
            return JsonSerializer.Deserialize<T>(json, options ?? serializerOptions);
        }

        public static object FromJson(this string json, Type type, JsonSerializerOptions options = default)
        {
            return JsonSerializer.Deserialize(json, type, options ?? serializerOptions);
        }

        public static T FromJson<T>(this byte[] utf8Json, JsonSerializerOptions options = default) where T : class, new()
        {
            return JsonSerializer.Deserialize<T>(utf8Json, options ?? serializerOptions);
        }

        public static object FromJson(this byte[] utf8Json, Type type, JsonSerializerOptions options = default)
        {
            return JsonSerializer.Deserialize(utf8Json, type, options ?? serializerOptions);
        }

        public static T FromJson<T>(this JsonElement jsonElement, JsonSerializerOptions options = default) where T : class, new()
        {
            return jsonElement.Deserialize<T>(options ?? serializerOptions);
        }

        public static object FromJson(this JsonElement jsonElement, Type type, JsonSerializerOptions options = default)
        {
            return jsonElement.Deserialize(type, options ?? serializerOptions);
        }
    }
}
