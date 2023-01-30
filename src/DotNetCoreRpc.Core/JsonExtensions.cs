﻿using System;
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

        public static string ToJson<T>(this T data) where T:class,new()
        {
            return JsonSerializer.Serialize(data, serializerOptions);
        }

        public static T FromJson<T>(this string json) where T : class, new()
        {
            return JsonSerializer.Deserialize<T>(json, serializerOptions);
        }

        public static object FromJson(this string json,Type type)
        {
            return JsonSerializer.Deserialize(json, type, serializerOptions);
        }

        public static T FromJson<T>(this byte[] utf8Json) where T : class, new()
        {
            return JsonSerializer.Deserialize<T>(utf8Json, serializerOptions);
        }

        public static object FromJson(this byte[] utf8Json, Type type)
        {
            return JsonSerializer.Deserialize(utf8Json, type, serializerOptions);
        }

        public static T FromJson<T>(this JsonElement jsonElement) where T : class, new()
        {
            return jsonElement.Deserialize<T>(serializerOptions);
        }

        public static object FromJson(this JsonElement jsonElement, Type type)
        {
            return jsonElement.Deserialize(type, serializerOptions);
        }
    }
}
