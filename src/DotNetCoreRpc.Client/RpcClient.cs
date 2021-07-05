﻿using System;
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
            HttpRequestInterceptor httpRequestInterceptor = new HttpRequestInterceptor(_httpClientFactory.CreateClient(serviceName));
            return _proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(httpRequestInterceptor);
        }
    }
}
