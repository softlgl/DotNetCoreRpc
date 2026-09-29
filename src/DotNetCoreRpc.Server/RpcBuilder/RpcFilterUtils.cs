using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCoreRpc.Server.RpcBuilder
{
    public static class RpcFilterUtils
    {
        // 缓存方法对应的过滤器标记（Type + Order），不可变，跨请求共享
        private static readonly ConcurrentDictionary<string, RpcFilterAttribute[]> _methodFilterAttributes = new ConcurrentDictionary<string, RpcFilterAttribute[]>();
        // 缓存过滤器类型的 ObjectFactory（ActivatorUtilities.CreateFactory），避免每请求走 CreateInstance 慢路径
        private static readonly ConcurrentDictionary<Type, ObjectFactory> _filterFactories = new ConcurrentDictionary<Type, ObjectFactory>();

        /// <summary>
        /// 获取方法上声明的过滤器标记（类级 + 方法级，按 <see cref="RpcFilterAttribute.Order"/> 稳定排序，
        /// 同 Order 类级在前、方法级在后）。全局过滤器不在此处，由 RpcServerOptions 单独维护。
        /// </summary>
        public static RpcFilterAttribute[] GetFilterAttributes(RpcContext aspectContext)
        {
            var methodInfo = aspectContext.Method;
            var methodKey = GetMethodKey(methodInfo);

            return _methodFilterAttributes.GetOrAdd(methodKey, key =>
            {
                var methodAttributes = methodInfo.GetCustomAttributes(true)
                    .Where(i => i is RpcFilterAttribute)
                    .Cast<RpcFilterAttribute>();
                var classAttributes = methodInfo.DeclaringType.GetCustomAttributes(true)
                    .Where(i => i is RpcFilterAttribute)
                    .Cast<RpcFilterAttribute>();

                return classAttributes.Concat(methodAttributes)
                    .OrderBy(attribute => attribute.Order)
                    .ToArray();
            });
        }

        /// <summary>
        /// 创建过滤器实例：优先取 DI 注册的实例（尊重其生命周期与作用域）；
        /// 未注册则用缓存好的 ObjectFactory 做构造函数注入，每次调用一个全新实例，天然线程安全。
        /// </summary>
        public static IRpcFilter CreateFilterInstance(IServiceProvider serviceProvider, Type filterType)
        {
            var instance = serviceProvider.GetService(filterType);
            if (instance is IRpcFilter filter)
            {
                return filter;
            }

            var factory = _filterFactories.GetOrAdd(filterType, type => ActivatorUtilities.CreateFactory(type, Type.EmptyTypes));
            return (IRpcFilter)factory(serviceProvider, Array.Empty<object>());
        }

        private static string GetMethodKey(MethodInfo methodInfo)
        {
            var reflectedTypeHandle = methodInfo.ReflectedType!.TypeHandle.Value;
            var methodHandle = methodInfo.MethodHandle.Value;
            return $"{reflectedTypeHandle}_{methodHandle}";
        }
    }
}
