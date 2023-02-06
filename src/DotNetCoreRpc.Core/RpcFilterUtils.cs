using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotNetCoreRpc.Core.RpcBuilder;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCoreRpc.Core
{
    public static class RpcFilterUtils
    {
        private static readonly ConcurrentDictionary<string, IEnumerable<PropertyInfo>> _filterFromServices = new ConcurrentDictionary<string, IEnumerable<PropertyInfo>>();
        private static readonly ConcurrentDictionary<string, List<RpcFilterAttribute>> _methodFilters = new ConcurrentDictionary<string, List<RpcFilterAttribute>>();

        /// <summary>
        /// 获取方法filters
        /// </summary>
        /// <returns></returns>
        public static List<RpcFilterAttribute> GetFilterAttributes(RpcContext aspectContext, IEnumerable<Type> filterTypes)
        {
            var methondInfo = aspectContext.Method;

            var methondInterceptorAttributes = _methodFilters.GetOrAdd($"{methondInfo.DeclaringType.FullName}#{methondInfo.Name}",
                key => {
                    var methondAttributes = methondInfo.GetCustomAttributes(true)
                                   .Where(i => typeof(RpcFilterAttribute).IsAssignableFrom(i.GetType()))
                                   .Cast<RpcFilterAttribute>().ToList();
                    var classAttributes = methondInfo.DeclaringType.GetCustomAttributes(true)
                        .Where(i => typeof(RpcFilterAttribute).IsAssignableFrom(i.GetType()))
                        .Cast<RpcFilterAttribute>();
                    //获取方法filter
                    methondAttributes.AddRange(classAttributes);
                    //获取全局filter
                    var glableInterceptorAttribute = GetInstances(aspectContext.HttpContext.RequestServices, filterTypes);
                    methondAttributes.AddRange(glableInterceptorAttribute);
                    return methondAttributes;
                });

            //filter属性注入
            PropertiesInject(aspectContext.HttpContext.RequestServices, methondInterceptorAttributes);
            return methondInterceptorAttributes;
        }

        private static IEnumerable<RpcFilterAttribute> GetInstances(IServiceProvider serviceProvider, IEnumerable<Type> filterTypes)
        {
            foreach (var filterType in filterTypes)
            {
                yield return GetInstance(serviceProvider, filterType);
            }
        }

        private static RpcFilterAttribute GetInstance(IServiceProvider serviceProvider, Type filterType)
        {
            return ActivatorUtilities.CreateInstance(serviceProvider, filterType) as RpcFilterAttribute;
        }

        private static void PropertiesInject(IServiceProvider serviceProvider, IEnumerable<RpcFilterAttribute> rpcFilterAttributes)
        {
            foreach (var fitler in rpcFilterAttributes)
            {
                PropertieInject(serviceProvider, fitler);
            }
        }

        private static void PropertieInject(IServiceProvider serviceProvider, RpcFilterAttribute rpcFilterAttribute)
        {
            var properties = _filterFromServices.GetOrAdd($"{rpcFilterAttribute.GetType().FullName}", key => rpcFilterAttribute.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(i => i.GetCustomAttribute<FromServicesAttribute>() != null));
            if (properties.Any())
            {
                foreach (var propertyInfo in properties)
                {
                    propertyInfo.SetValue(rpcFilterAttribute, serviceProvider.GetService(propertyInfo.PropertyType));
                }
            }
        }
    }
}
