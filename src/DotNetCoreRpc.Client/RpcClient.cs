using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace DotNetCoreRpc.Client
{
    internal class RpcClient
    {
        private HttpClient _httpClient;
        private ProxyGenerator _proxyGenerator;
        private string _path;

        public RpcClient(HttpClient httpClient, ProxyGenerator proxyGenerator, string path)
        {
            _httpClient = httpClient;
            _proxyGenerator = proxyGenerator;
            _path = path;
        }

        internal T CreateClient<T>() where T : class
        {
            InitalHttpClient();

            HttpRequestInterceptor httpRequestInterceptor = new HttpRequestInterceptor(_httpClient);
            return _proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(httpRequestInterceptor);
        }

        private void InitalHttpClient()
        {
            _httpClient.DefaultRequestHeaders.Add("req-source", "dncrpc");
            if (!string.IsNullOrWhiteSpace(_path))
            {
                string path = _path;

                if (_path.StartsWith("/"))
                {
                    path = path.TrimStart('/');
                }

                _httpClient.BaseAddress = new Uri(_httpClient.BaseAddress.ToString() + path);
            }
        }
    }
}
