using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotNetCoreRpc.Core.RpcBuilder;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCoreRpc.Core
{
    public static class RpcFilterUtils
    {
        public static RpcFilterAttribute GetInstance(IServiceProvider serviceProvider,Type filterType)
        {
            return ActivatorUtilities.CreateInstance(serviceProvider, filterType) as RpcFilterAttribute;
        }

        public static IEnumerable<RpcFilterAttribute> GetInstances(IServiceProvider serviceProvider, IEnumerable<Type> filterTypes)
        {
            foreach (var filterType in filterTypes)
            {
                yield return GetInstance(serviceProvider, filterType);
            }
        }

        public static void PropertieInject(IServiceProvider serviceProvider, RpcFilterAttribute rpcFilterAttribute)
        {
            var properties = rpcFilterAttribute.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(i=>i.GetCustomAttribute<FromServicesAttribute>()!=null);
            
            if (properties.Any())
            {
                foreach (var propertyInfo in properties)
                {
                    propertyInfo.SetValue(rpcFilterAttribute, serviceProvider.GetService(propertyInfo.PropertyType));
                }
            }
        }

        public static void PropertiesInject(IServiceProvider serviceProvider, IEnumerable<RpcFilterAttribute> rpcFilterAttributes)
        {
            foreach (var fitler in rpcFilterAttributes)
            {
                PropertieInject(serviceProvider, fitler);
            }
        }
    }
}
