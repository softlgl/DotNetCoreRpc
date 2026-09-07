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

        // 未找到类型的占位符：ConcurrentDictionary 不允许 null value，用此类型实例标记"已查过但未找到"，
        // 避免每次未注册服务名的请求都重新跑全程序集扫描
        private static readonly Type NotFoundMarker = typeof(NotFoundTypeMarker);
        private sealed class NotFoundTypeMarker { }

        internal Type this[string serviceName]
        {
            get { return GetServiceType(serviceName); }
        }

        internal Type GetServiceType(string serviceName)
        {
            var type = _serviceTypes.GetOrAdd(serviceName, typeName => {
                // 每个服务名只会在 miss 时扫描一次（结果进缓存），直接扫当前已加载程序集，
                // 不做快照缓存以便能看到后续动态加载的程序集
                Type serviceType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(typeName)).FirstOrDefault(t => t != null);
                return serviceType ?? NotFoundMarker;
            });

            if (type == NotFoundMarker)
            {
                throw new InvalidOperationException($"未能找到服务类型:{serviceName}的定义");
            }

            return type;
        }
    }
}
