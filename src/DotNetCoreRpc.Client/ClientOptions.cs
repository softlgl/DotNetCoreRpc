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

        /// <summary>
        /// 服务请求路径
        /// </summary>
        public string Path { get; set; }

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
                var httpClient = httpClientFactory.CreateClient(_serviceName);
                RpcClient rpcClient = new RpcClient(httpClient, proxyGenerator, Path);
                return rpcClient.CreateClient<T>();
            });
            return this;
        }
    }
}
