using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetCoreRpc.Core;
using DotNetCoreRpc.Core.RpcBuilder;
using Test.Service.Configs;

namespace Test.Service.Filters
{
    public class LoggerFilter:RpcFilterAttribute
    {
        [FromServices]
        private RedisConfig RedisConfig { get; set; }

        [FromServices]
        private ElasticSearchConfig ElasticSearchConfig { get; set; }

        public override async Task InvokeAsync(RpcContext context, RpcRequestDelegate next)
        {
            Debug.WriteLine($"LoggerFilter begin,Parameters={context.Parameters[0].ToJson()}");
            await next(context);
            Debug.WriteLine($"LoggerFilter end,ReturnValue={context.ReturnValue.ToJson()}");
        }
    }
}
