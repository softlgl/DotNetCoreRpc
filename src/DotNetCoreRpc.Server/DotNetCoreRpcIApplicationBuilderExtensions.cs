using System;
using Microsoft.AspNetCore.Builder;

namespace DotNetCoreRpc.Server
{
    public static class DotNetCoreRpcIApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseDotNetCoreRpc(this IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseMiddleware<DotNetCoreRpcMiddleware>();
            return applicationBuilder;
        }
    }
}
