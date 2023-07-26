using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;

namespace DotNetCoreRpc.Client
{
    public class ClientOptions
    {
        /// <summary>
        /// 服务请求路径
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// 服务名称
        /// </summary>
        public string ServiceName { get; }

        public IServiceCollection Services { get; }

        public ClientOptions(IServiceCollection services, string serviceName)
        {
            Services = services;
            ServiceName = serviceName;
        }

        public ClientOptions AddRpcClient<T>() where T : class
        {
            Services.TryAddScoped(provider => CreateRpcClient(provider));

            T CreateRpcClient(IServiceProvider serviceProvider)
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var proxyGenerator = serviceProvider.GetRequiredService<ProxyGenerator>();
                var httpClient = httpClientFactory.CreateClient(ServiceName);

                RpcClient rpcClient = new RpcClient(this, httpClient, proxyGenerator);
                return rpcClient.CreateClient<T>();
            }

            return this;
        }
    }
}
