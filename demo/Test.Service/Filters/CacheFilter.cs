using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetCoreRpc.Core;
using DotNetCoreRpc.Server.RpcBuilder;
using Microsoft.Extensions.Logging;
using Test.Service.Configs;

namespace Test.Service.Filters
{
    public class CacheFilter : RpcFilterAttribute
    {
        //输出 JSON 时保持汉字原样（不转义为 \uXXXX）
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        //private readonly ElasticSearchConfig _elasticSearchConfig;

        //[FromServices]
        private RedisConfig RedisConfig { get; set; }

#if NET8_0_OR_GREATER
        [FromServices("elasticSearchConfig")]
        private ElasticSearchConfig ElasticSearchConfig { get; set; }
#endif

        [FromServices]
        private ILogger<CacheFilter> Logger { get; set; }

        //public CacheFilter(ElasticSearchConfig elasticSearchConfig)
        //{
        //    _elasticSearchConfig = elasticSearchConfig;
        //}

        public override async Task InvokeAsync(RpcContext context, RpcRequestDelegate next)
        {
            Logger.LogInformation("CacheFilter begin, Parameters={Parameters}", JsonSerializer.Serialize(context.Parameters, JsonOptions));
            await next(context);
            Logger.LogInformation("CacheFilter end, ReturnValue={ReturnValue}", JsonSerializer.Serialize(context.ReturnValue, JsonOptions));
        }
    }
}
