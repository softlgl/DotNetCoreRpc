using Microsoft.Extensions.Logging;
using Nacos;
using Nacos.V2;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test.Client
{
    public class NacosDiscoveryDelegatingHandler : DelegatingHandler
    {
        private readonly INacosNamingService _serverManager;
        private readonly ILogger<NacosDiscoveryDelegatingHandler> _logger;

        public NacosDiscoveryDelegatingHandler(INacosNamingService serverManager, ILogger<NacosDiscoveryDelegatingHandler> logger)
        {
            _serverManager = serverManager;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var current = request.RequestUri;
            try
            {
                //通过nacos sdk获取注册中心服务地址，内置了随机负载均衡算法，所以只返回一条信息
                var instance = await _serverManager.SelectOneHealthyInstance(current.Host);
                request.RequestUri = new Uri($"http://{instance.Ip}:{instance.Port}{current.PathAndQuery}");
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger?.LogDebug(e, "Exception during SendAsync()");
                throw;
            }
            finally
            {
                request.RequestUri = current;
            }
        }
    }
}
