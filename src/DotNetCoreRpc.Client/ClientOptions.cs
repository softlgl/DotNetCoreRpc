using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
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

        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;

#if NET8_0_OR_GREATER
        public bool AddAsKeyed { get; set; } = false;
#endif

        public ClientOptions(IServiceCollection services, string serviceName)
        {
            Services = services;
            ServiceName = serviceName;
        }

        public ClientOptions AddRpcClient<T>(ServiceLifetime? lifetime = null) where T : class
        {
#if NET8_0_OR_GREATER
            if (AddAsKeyed)
            {
                Services.Add(ServiceDescriptor.DescribeKeyed(typeof(T), ServiceName, (provider, _) => CreateRpcClient(provider), lifetime ?? Lifetime));
            }
            else
            {
#endif
            Services.Add(ServiceDescriptor.Describe(typeof(T), provider => CreateRpcClient(provider), lifetime ?? Lifetime));
#if NET8_0_OR_GREATER
            }
#endif

            T CreateRpcClient(IServiceProvider serviceProvider)
            {
                var proxyGenerator = serviceProvider.GetRequiredService<ProxyGenerator>();
                var httpClient = HttpClientFactoryHelper.CreateHttpClient(serviceProvider, ServiceName, Path);

                RpcClient rpcClient = new RpcClient(httpClient, proxyGenerator);
                return rpcClient.CreateClient<T>();
            }

            return this;
        }
    }
 }
