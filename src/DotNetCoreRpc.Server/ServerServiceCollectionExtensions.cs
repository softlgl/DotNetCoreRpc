using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetCoreRpc.Server
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDotNetCoreRpcServer(this IServiceCollection services, Action<RpcServerOptions> options = null)
        {
            RpcServerOptions rpcServerOptions = new RpcServerOptions();
            if (options != null)
            {
                options.Invoke(rpcServerOptions);
            }

            services.TryAddSingleton(rpcServerOptions);
            return services;
        }
    }
}
