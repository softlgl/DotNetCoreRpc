using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCoreRpc.Server
{
    public class RpcServerOptions
    {
        private readonly IDictionary<string, Type> _types = new Dictionary<string, Type>();
        private readonly IServiceCollection _services;
        public RpcServerOptions(IServiceCollection services)
        {
            _services = services;
        }

        public RpcServerOptions AddService<TService>()
            where TService:class
        {
            Type serviceType = typeof(TService);
            _types.TryAdd(serviceType.FullName, serviceType);
            return this;
        }

        public IDictionary<string, Type> GetTypes()
        {
            return _types.ToDictionary(i=>i.Key,i=>i.Value);
        }
    }
}
