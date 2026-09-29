using System;

namespace DotNetCoreRpc.Server.RpcBuilder
{
    /// <summary>
    /// 声明性标记：把 <see cref="FilterType"/> 指示的过滤器挂到服务类或方法上。
    /// 过滤器本体是普通类（见 <see cref="IRpcFilter"/>），由依赖注入容器解析，支持构造函数注入。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class RpcFilterAttribute : Attribute
    {
        /// <summary>过滤器实现类型，需实现 <see cref="IRpcFilter"/> 接口。</summary>
        public Type FilterType { get; }

        /// <summary>
        /// 过滤器执行顺序，值小的先执行（仅在同级范围内排序：类/方法；
        /// 整体层级为 全局 -> 类 -> 方法，与 ASP.NET Core MVC 过滤器惯例一致）
        /// </summary>
        public int Order { get; set; }

        public RpcFilterAttribute(Type filterType)
        {
            FilterType = filterType ?? throw new ArgumentNullException(nameof(filterType));
        }
    }
}
