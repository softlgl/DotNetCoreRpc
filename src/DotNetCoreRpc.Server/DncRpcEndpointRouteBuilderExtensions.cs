using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCoreRpc.Server
{
    /// <summary>
    /// DotNetCoreRpc服务终结点扩展类
    /// </summary>
    public static class DncRpcEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapDotNetCoreRpc(this IEndpointRouteBuilder endpoints, string path = default)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            path = string.IsNullOrWhiteSpace(path) ? "/DotNetCoreRpc/ServerRequest" : path;
            RequestDelegate requestDelegate = endpoints.CreateApplicationBuilder().UseDotNetCoreRpc(path).Build();
            return endpoints.MapPost(path, requestDelegate);
        }
    }
}
