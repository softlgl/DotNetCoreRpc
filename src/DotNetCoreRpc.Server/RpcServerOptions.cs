using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCoreRpc.Core.RpcBuilder;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCoreRpc.Server
{
    public class RpcServerOptions
    {
        private readonly IDictionary<string, Type> _types = new Dictionary<string, Type>();
        private readonly IList<Type> _filterTypes = new List<Type>();
        private readonly IServiceCollection _services;

        public RpcServerOptions(IServiceCollection services)
        {
            _services = services;
        }

        public RpcServerOptions AddFilter<RpcFilterAttribute>()
        {
            _filterTypes.Add(typeof(RpcFilterAttribute));
            return this;
        }

        public RpcServerOptions AddService<TService>()
            where TService:class
        {
            Type serviceType = typeof(TService);
            _types.TryAdd(serviceType.FullName, serviceType);
            return this;
        }

        public RpcServerOptions AddService(string serviceName)
        {
            foreach (var service in _services)
            {
                if (serviceName.StartsWith("*")
                    && service.ServiceType.Name.EndsWith(serviceName.Substring(1)))
                {
                    _types.TryAdd(service.ServiceType.FullName, service.ServiceType);
                    continue;
                }
                if (service.ServiceType.Name == serviceName)
                {
                    _types.TryAdd(service.ServiceType.FullName, service.ServiceType);
                }
            }
            return this;
        }

        public RpcServerOptions AddNameSpace(string nameSpace)
        {
            foreach (var service in _services)
            {
                if (service.ServiceType.Namespace == nameSpace)
                {
                    _types.TryAdd(service.ServiceType.FullName, service.ServiceType);
                }
            }
            return this;
        }

        public IDictionary<string, Type> GetTypes()
        {
            return _types.ToDictionary(i=>i.Key,i=>i.Value);
        }

        public IEnumerable<Type> GetFilterTypes()
        {
            foreach (var filterType in _filterTypes)
            {
                yield return filterType;
            }
        } 
    }
}
