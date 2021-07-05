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
        public static IEndpointConventionBuilder MapDotNetCoreRpc(this IEndpointRouteBuilder endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }
            RequestDelegate requestDelegate = endpoints.CreateApplicationBuilder().UseDotNetCoreRpc().Build();
            return endpoints.MapPost("/DotNetCoreRpc/ServerRequest", requestDelegate);
        }
    }
}
