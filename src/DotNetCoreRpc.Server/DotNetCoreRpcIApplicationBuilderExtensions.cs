using System;
using Microsoft.AspNetCore.Builder;

namespace DotNetCoreRpc.Server
{
    public static class DotNetCoreRpcIApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseDotNetCoreRpc(this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder.UseWhen(context => context.Request.Path.Value.Contains("/DotNetCoreRpc/ServerRequest"),
            appBuilder => appBuilder.UseMiddleware<DotNetCoreRpcMiddleware>());
        }
    }
}
