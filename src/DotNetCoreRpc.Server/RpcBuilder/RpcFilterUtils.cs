using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCoreRpc.Server.RpcBuilder
{
    public static class RpcFilterUtils
    {
        // 缓存方法对应的过滤器模板实例（含特性配置，不可变，绝不在模板上做属性注入）
        private static readonly ConcurrentDictionary<string, List<RpcFilterAttribute>> _methodFilterTemplates = new ConcurrentDictionary<string, List<RpcFilterAttribute>>();
        // 缓存过滤器类型的 ObjectFactory（ActivatorUtilities.CreateFactory），避免每请求走 CreateInstance 慢路径
        private static readonly ConcurrentDictionary<Type, ObjectFactory> _filterFactories = new ConcurrentDictionary<Type, ObjectFactory>();
        // 缓存过滤器类型上需复制的配置属性（非 [FromServices] 的可读写属性），避免每请求 GetProperties + GetCustomAttribute
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _configuredProperties = new ConcurrentDictionary<Type, PropertyInfo[]>();
        // 缓存过滤器类型上 [FromServices] 属性及其特性（含 ServiceKey），避免每请求重复枚举与取特性
        private static readonly ConcurrentDictionary<Type, (PropertyInfo Property, FromServicesAttribute Attribute)[]> _fromServicesProperties = new ConcurrentDictionary<Type, (PropertyInfo, FromServicesAttribute)[]>();

        /// <summary>
        /// 获取方法的过滤器模板列表（按方法缓存，跨请求共享且不可变）。
        /// 模板用于保留特性上配置的属性（如 Order），每个请求再通过 <see cref="CreateFilterInstance"/> 创建独立实例。
        /// 执行层级与 ASP.NET Core MVC 惯例一致：全局 -> 类 -> 方法，同级内按 <see cref="RpcFilterAttribute.Order"/> 升序（稳定排序，同 Order 保持声明顺序）。
        /// </summary>
        public static List<RpcFilterAttribute> GetFilterTemplates(RpcContext aspectContext, IServiceProvider serviceProvider, IEnumerable<Type> filterTypes)
        {
            var methodInfo = aspectContext.Method;
            var methodKey = GetMethodKey(methodInfo);

            return _methodFilterTemplates.GetOrAdd(methodKey, key =>
            {
                var methodAttributes = methodInfo.GetCustomAttributes(true)
                    .Where(i => i is RpcFilterAttribute)
                    .Cast<RpcFilterAttribute>().ToList();
                var classAttributes = methodInfo.DeclaringType.GetCustomAttributes(true)
                    .Where(i => i is RpcFilterAttribute)
                    .Cast<RpcFilterAttribute>().ToList();
                var globalAttributes = GetInstances(serviceProvider, filterTypes).ToList();

                return globalAttributes.OrderBy(f => f.Order)
                    .Concat(classAttributes.OrderBy(f => f.Order))
                    .Concat(methodAttributes.OrderBy(f => f.Order))
                    .ToList();
            });
        }

        /// <summary>
        /// 为每个请求基于模板创建独立的过滤器实例：复制特性配置属性，并按当前请求作用域注入 [FromServices] 服务。
        /// 工厂与属性元数据均按类型缓存，热路径仅剩属性赋值与服务解析。
        /// </summary>
        public static RpcFilterAttribute CreateFilterInstance(IServiceProvider serviceProvider, RpcFilterAttribute template)
        {
            var filterType = template.GetType();
            var factory = _filterFactories.GetOrAdd(filterType, type => ActivatorUtilities.CreateFactory(type, Type.EmptyTypes));
            var instance = factory(serviceProvider, Array.Empty<object>()) as RpcFilterAttribute;
            if (instance == null)
            {
                return template;
            }

            CopyConfiguredProperties(template, instance);
            PropertyInject(serviceProvider, instance);
            return instance;
        }

        private static string GetMethodKey(MethodInfo methodInfo)
        {
            var reflectedTypeHandle = methodInfo.ReflectedType!.TypeHandle.Value;
            var methodHandle = methodInfo.MethodHandle.Value;
            return $"{reflectedTypeHandle}_{methodHandle}";
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

        /// <summary>
        /// 复制模板上非 [FromServices] 的可读写属性（如 Order 等特性配置），避免新建实例丢失配置。
        /// 属性列表按类型缓存，避免每请求反射 GetProperties/GetCustomAttribute。
        /// </summary>
        private static void CopyConfiguredProperties(RpcFilterAttribute template, RpcFilterAttribute instance)
        {
            var properties = _configuredProperties.GetOrAdd(template.GetType(), type => type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0 && p.GetCustomAttribute<FromServicesAttribute>() == null)
                .ToArray());
            foreach (var propertyInfo in properties)
            {
                propertyInfo.SetValue(instance, propertyInfo.GetValue(template));
            }
        }

        private static void PropertyInject(IServiceProvider serviceProvider, RpcFilterAttribute rpcFilterAttribute)
        {
            // 按类型缓存 (属性, 特性) 数组：一次反射完成，热路径直接遍历，避免旧的 lazy IEnumerable 双重枚举与循环内重复取特性
            var properties = _fromServicesProperties.GetOrAdd(rpcFilterAttribute.GetType(), type => type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(p => (Property: p, Attribute: p.GetCustomAttribute<FromServicesAttribute>()))
                .Where(i => i.Attribute != null)
                .ToArray());
            foreach (var (propertyInfo, serviceAttribute) in properties)
            {
#if NET8_0_OR_GREATER
                if (!string.IsNullOrWhiteSpace(serviceAttribute.SeviceKey))
                {
                    propertyInfo.SetValue(rpcFilterAttribute, serviceProvider.GetRequiredKeyedService(propertyInfo.PropertyType, serviceAttribute.SeviceKey));
                    continue;
                }
#endif
                propertyInfo.SetValue(rpcFilterAttribute, serviceProvider.GetRequiredService(propertyInfo.PropertyType));
            }
        }
    }
}
