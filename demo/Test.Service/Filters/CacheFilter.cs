using System;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using DotNetCoreRpc.Core;
using DotNetCoreRpc.Server.RpcBuilder;
using Microsoft.Extensions.Logging;
using Test.Service.Configs;

namespace Test.Service.Filters
{
    public class CacheFilter : RpcFilterAttribute
    {
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
            Logger.LogInformation($"CacheFilter begin,Parameters={context.Parameters}");
            await next(context);
            Logger.LogInformation($"CacheFilter end,ReturnValue={context.ReturnValue.ToJson()}");
        }
    }
}
