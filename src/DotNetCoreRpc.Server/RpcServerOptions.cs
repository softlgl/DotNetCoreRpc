using System;
using System.Collections.Generic;

namespace DotNetCoreRpc.Server
{
    public class RpcServerOptions
    {
        private readonly IList<Type> _filterTypes = new List<Type>();
        private readonly ServiceTypeCollection _serviceTypes = new ServiceTypeCollection();

        public RpcServerOptions AddFilter<RpcFilterAttribute>()
        {
            _filterTypes.Add(typeof(RpcFilterAttribute));
            return this;
        }

        internal IEnumerable<Type> GetFilterTypes()
        {
            return _filterTypes;
        }

        internal Type GetServiceType(string serviceName)
        {
            return _serviceTypes[serviceName];
        }
    }
}
