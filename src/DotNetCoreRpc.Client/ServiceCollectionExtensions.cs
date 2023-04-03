using System;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DotNetCoreRpc.Client
{
    public static class ServiceCollectionExtensions
    {
        public static IHttpClientBuilder AddDotNetCoreRpcClient(this IHttpClientBuilder httpClientBuilder, Action<ClientOptions> options)
        {
            httpClientBuilder.Services.TryAddSingleton<ProxyGenerator>();

            ClientOptions clientOptions = new ClientOptions(httpClientBuilder.Services, httpClientBuilder.Name);
            options.Invoke(clientOptions);

            return httpClientBuilder;
        }
    }
}
