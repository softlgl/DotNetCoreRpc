using System;
using Microsoft.AspNetCore.Builder;

namespace DotNetCoreRpc.Server
{
    public static class DotNetCoreRpcIApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseDotNetCoreRpc(this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder.UseWhen(context => context.Request.Path.Value.Contains("/DotNetCoreRpc/Server")
                && context.Request.Headers.ContainsKey("User-Agent")
                && context.Request.Headers["User-Agent"] == "DotNetCoreRpc.Client",
            appBuilder => appBuilder.UseMiddleware<DotNetCoreRpcMiddleware>());
        }
    }
}
