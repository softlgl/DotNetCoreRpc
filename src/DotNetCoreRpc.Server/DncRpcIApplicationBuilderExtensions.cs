using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;

namespace DotNetCoreRpc.Server
{
    public static class DncRpcIApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseDotNetCoreRpc(this IApplicationBuilder applicationBuilder, string path = default)
        {
            path = string.IsNullOrWhiteSpace(path) ? "/DotNetCoreRpc/ServerRequest" : path;
            var normalizedPath = path.TrimEnd('/');
            return applicationBuilder.UseWhen(context =>
            {
                // 精确匹配或按段前缀匹配，避免旧 Contains 把 /xrpcfoo 误判为 /rpc
                var requestPath = context.Request.Path.Value?.TrimEnd('/');
                return (requestPath == normalizedPath || requestPath != null && requestPath.StartsWith(normalizedPath + "/"))
                    && context.Request.Headers.ContainsKey("req-source")
                    && context.Request.Headers["req-source"] == "dncrpc"
                    && string.Equals(context.Request.Method, "post", StringComparison.OrdinalIgnoreCase);
            },
            appBuilder => appBuilder.UseMiddleware<DotNetCoreRpcMiddleware>());
        }
    }
}
