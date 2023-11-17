using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCoreRpc.Core;
using DotNetCoreRpc.Server.RpcBuilder;
using Microsoft.Extensions.Logging;
using Test.Service.Configs;

namespace Test.Service.Filters
{
    public class LoggerFilter:RpcFilterAttribute
    {
        [FromServices]
        private RedisConfig RedisConfig { get; set; }

#if NET8_0_OR_GREATER
        [FromServices("elasticSearchConfig")]
        private ElasticSearchConfig ElasticSearchConfig { get; set; }
#endif

        [FromServices]
        private ILogger<CacheFilter> Logger { get; set; }

        public override async Task InvokeAsync(RpcContext context, RpcRequestDelegate next)
        {
            Logger.LogInformation($"LoggerFilter begin,Parameters={context.Parameters[0].ToJson()}");
            await next(context);
            Logger.LogInformation($"LoggerFilter end,ReturnValue={context.ReturnValue.ToJson()}");
        }
    }
}
