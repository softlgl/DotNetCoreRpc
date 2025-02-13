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

        private static void InitialHttpClient(HttpClient httpClient, string path)
        {
            if (httpClient.DefaultRequestHeaders.Contains("req-source"))
            {
                return;
            }

            httpClient.DefaultRequestHeaders.Add("req-source", "dncrpc");
            if (!string.IsNullOrWhiteSpace(path))
            {
                if (path.StartsWith("/"))
                {
                    path = path.TrimStart('/');
                }

                httpClient.BaseAddress = new Uri(httpClient.BaseAddress.ToString() + path);
            }
        }
    }
}
