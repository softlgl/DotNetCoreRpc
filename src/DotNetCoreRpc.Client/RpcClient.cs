using System;
using System.Net.Http;
using Castle.DynamicProxy;
using System.Collections.Concurrent;

namespace DotNetCoreRpc.Client
{
    public class RpcClient
    {
        private readonly ProxyGenerator _proxyGenerator;
        private readonly IHttpClientFactory _httpClientFactory;

        public RpcClient(ProxyGenerator proxyGenerator, IHttpClientFactory httpClientFactory)
        {
            _proxyGenerator = proxyGenerator;
            _httpClientFactory = httpClientFactory;
        }

        public T CreateClient<T>(string serviceName) where T : class
        {
            return CreateClient<T>(_httpClientFactory.CreateClient(serviceName));
        }

        public T CreateClient<T>(HttpClient httpClient) where T : class
        {
            HttpRequestInterceptor httpRequestInterceptor = new HttpRequestInterceptor(httpClient);
            return _proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(httpRequestInterceptor);
        }
    }
}
