using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace DotNetCoreRpc.Client
{
    public class ClientOptions
    {
        private IServiceCollection _services;
        private readonly string _serviceName;

        public ClientOptions(IServiceCollection services, string serviceName)
        {
            _services = services;
            _serviceName = serviceName;
        }

        public ClientOptions AddRpcClient<T>() where T : class
        {
            _services.TryAddScoped(provider => {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var proxyGenerator = provider.GetRequiredService<ProxyGenerator>();
                RpcClient rpcClient = new RpcClient(httpClientFactory.CreateClient(_serviceName), proxyGenerator);
                return rpcClient.CreateClient<T>();
            });
            return this;
        }
    }
}
