using System;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCoreRpc.Client
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDotNetCoreRpcClient(this IServiceCollection services)
        {
            services.AddSingleton<ProxyGenerator>();
            services.AddSingleton<RpcClient>();
            return services;
        }
    }
}
