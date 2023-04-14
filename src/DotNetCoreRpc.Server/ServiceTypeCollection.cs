using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace DotNetCoreRpc.Server
{
    internal class ServiceTypeCollection
    {
        private readonly ConcurrentDictionary<string, Type> _serviceTypes = new ConcurrentDictionary<string, Type>();
        private readonly Lazy<Assembly[]> _assemblies = new Lazy<Assembly[]>(() => AppDomain.CurrentDomain.GetAssemblies(), true);

        internal Type this[string serviceName]
        {
            get { return GetServiceType(serviceName); }
        }

        internal Type GetServiceType(string serviceName)
        {
            return _serviceTypes.GetOrAdd(serviceName, typeName => {
                Type serviceType = _assemblies.Value.Select(a => a.GetType(typeName)).FirstOrDefault(t => t != null);

                if (serviceType == null)
                {
                    throw new ArgumentNullException($"未能找到{typeName}的定义");
                }

                return serviceType;
            });
        }
    }
}
