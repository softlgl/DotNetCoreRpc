using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace DotNetCoreRpc.Client
{
    internal class RpcClient
    {
        private HttpClient _httpClient;
        private ProxyGenerator _proxyGenerator;

        public RpcClient(HttpClient httpClient, ProxyGenerator proxyGenerator)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("req-source", "dncrpc");
            _proxyGenerator = proxyGenerator;
        }

        internal T CreateClient<T>() where T : class
        {
            HttpRequestInterceptor httpRequestInterceptor = new HttpRequestInterceptor(_httpClient);
            return _proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(httpRequestInterceptor);
        }
    }
}
