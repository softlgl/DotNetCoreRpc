﻿using Castle.DynamicProxy;
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

        public RpcClient(ClientOptions clientOptions, HttpClient httpClient, ProxyGenerator proxyGenerator)
        {
            _path = clientOptions.Path;
            _httpClient = httpClient;
            _proxyGenerator = proxyGenerator;
        }

        internal T CreateClient<T>() where T : class
        {
            InitialHttpClient();

            return _proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(new HttpRequestInterceptor(_httpClient));
        }

        private void InitialHttpClient()
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
