using System;
using Microsoft.AspNetCore.Builder;

namespace DotNetCoreRpc.Server
{
    public static class DncRpcIApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseDotNetCoreRpc(this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder.UseWhen(context => context.Request.Path.Value.Contains("/DotNetCoreRpc/ServerRequest")
                && context.Request.Headers.ContainsKey("req-source")
                && context.Request.Headers["req-source"] == "dncrpc"
                && string.Equals(context.Request.Method, "post", StringComparison.OrdinalIgnoreCase), 
            appBuilder => appBuilder.UseMiddleware<DotNetCoreRpcMiddleware>());
        }
    }
}
