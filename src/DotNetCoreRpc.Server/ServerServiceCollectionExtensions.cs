using System;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCoreRpc.Server
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDotNetCoreRpcServer(this IServiceCollection services,Action<RpcServerOptions> options)
        {
            RpcServerOptions rpcServerOptions = new RpcServerOptions(services);
            options.Invoke(rpcServerOptions);
            services.AddSingleton(rpcServerOptions);
            return services;
        }
    }
}
