using System;
using Microsoft.AspNetCore.Builder;

namespace DotNetCoreRpc.Server
{
    public static class DotNetCoreRpcIApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseDotNetCoreRpc(this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder.UseWhen(context => string.Equals(context.Request.Method,"post",StringComparison.OrdinalIgnoreCase)
            && context.Request.Path.Value.Contains("/DotNetCoreRpc/ServerRequest"),
            appBuilder => appBuilder.UseMiddleware<DotNetCoreRpcMiddleware>());
        }
    }
}
