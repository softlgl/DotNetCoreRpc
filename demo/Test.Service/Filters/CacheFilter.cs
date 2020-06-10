using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCoreRpc.Core;
using DotNetCoreRpc.Core.RpcBuilder;
using Test.Service.Configs;

namespace Test.Service.Filters
{
    public class CacheFilter : RpcFilterAttribute
    {
        private readonly ElasticSearchConfig _elasticSearchConfig;

        [FromServices]
        private RedisConfig RedisConfig { get; set; }

        public CacheFilter(ElasticSearchConfig elasticSearchConfig)
        {
            _elasticSearchConfig = elasticSearchConfig;
        }
        public override async Task InvokeAsync(RpcContext context, RpcRequestDelegate next)
        {
            Debug.WriteLine($"CacheFilter begin,Parameters={context.Parameters}");
            await next(context);
            Debug.WriteLine($"CacheFilter end,ReturnValue={context.ReturnValue.ToJson()}");
        }
    }
}
