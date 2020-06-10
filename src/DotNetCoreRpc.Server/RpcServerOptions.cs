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
            return this;
        }

        public RpcServerOptions AddNameSpace(string nameSpace)
        {
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
