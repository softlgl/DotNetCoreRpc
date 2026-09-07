using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCoreRpc.Client
{
    public static class HttpClientFactoryHelper
    {
        public static HttpClient CreateHttpClient(IServiceProvider serviceProvider, string serviceName, string path)
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            HttpClient httpClient;
#if NET8_0_OR_GREATER
            httpClient = serviceProvider.GetKeyedService<HttpClient>(serviceName) ?? httpClientFactory.CreateClient(serviceName);
#else
            httpClient = httpClientFactory.CreateClient(serviceName);
#endif
            InitialHttpClient(httpClient, path);
            return httpClient;
        }

        // 初始化为低频操作，全局锁足够；避免多线程首次创建客户端时对共享 HttpClient 的 check-then-act 竞态
        private static readonly object _initLock = new object();

        private static void InitialHttpClient(HttpClient httpClient, string path)
        {
            lock (_initLock)
            {
                if (!httpClient.DefaultRequestHeaders.Contains("req-source"))
                {
                    httpClient.DefaultRequestHeaders.Add("req-source", "dncrpc");
                }

                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }

                if (path.StartsWith("/"))
                {
                    path = path.TrimStart('/');
                }

                if (httpClient.BaseAddress == null)
                {
                    throw new InvalidOperationException($"配置了 Path '{path}' 但 HttpClient.BaseAddress 为空，请先在 AddHttpClient 中设置 BaseAddress");
                }

                var baseUri = httpClient.BaseAddress.ToString();
                // 幂等：仅在 BaseAddress 尚未包含该 path 时追加，避免同一客户端重复初始化时多次拼接
                if (!baseUri.EndsWith(path))
                {
                    httpClient.BaseAddress = new Uri(baseUri + path);
                }
            }
        }
    }
}
