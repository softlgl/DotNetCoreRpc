using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCoreRpc.Core.RpcBuilder;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCoreRpc.Server
{
    public class RpcServerOptions
    {
        private readonly IDictionary<string, Type> _serviceTypes = new Dictionary<string, Type>();
        private readonly HashSet<Type> _serviceTypeSet = new HashSet<Type>();
        private readonly HashSet<string> _serviceNameSet = new HashSet<string>();
        private readonly HashSet<string> _serviceNameSpaceSet = new HashSet<string>();
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
            _serviceTypeSet.Add(typeof(TService));
            return this;
        }

        public RpcServerOptions AddService(string serviceName)
        {
            _serviceNameSet.Add(serviceName);
            return this;
        }

        public RpcServerOptions AddNameSpace(string nameSpace)
        {
            _serviceNameSpaceSet.Add(nameSpace);
            return this;
        }

        public IDictionary<string, Type> GetRegisterTypes()
        {
            foreach (var serviceType in _serviceTypeSet)
            {
                _serviceTypes.TryAdd(serviceType.FullName, serviceType);
            }

            foreach (var serviceName in _serviceNameSet)
            {
                string subServiceName = serviceName[1..];
                foreach (var service in _services)
                {
                    if (serviceName.StartsWith("*") && service.ServiceType.Name.EndsWith(subServiceName))
                    {
                        _serviceTypes.TryAdd(service.ServiceType.FullName, service.ServiceType);
                        continue;
                    }

                    if (service.ServiceType.Name == serviceName)
                    {
                        _serviceTypes.TryAdd(service.ServiceType.FullName, service.ServiceType);
                    }
                }
            }

            foreach (var nameSpace in _serviceNameSpaceSet)
            {
                foreach (var service in _services)
                {
                    if (service.ServiceType.Namespace == nameSpace)
                    {
                        _serviceTypes.TryAdd(service.ServiceType.FullName, service.ServiceType);
                    }
                }
            }

            return _serviceTypes.ToDictionary(i => i.Key, i => i.Value);
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
