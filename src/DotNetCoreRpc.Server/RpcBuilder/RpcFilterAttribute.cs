using System;
using System.Threading.Tasks;

namespace DotNetCoreRpc.Server.RpcBuilder
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public abstract class RpcFilterAttribute : Attribute
    {
        /// <summary>
        /// 过滤器执行顺序，值小的先执行（仅在同级范围内排序：全局/类/方法；
        /// 整体层级为 全局 -> 类 -> 方法，与 ASP.NET Core MVC 过滤器惯例一致）
        /// </summary>
        public int Order { get; set; }

        public abstract Task InvokeAsync(RpcContext context, RpcRequestDelegate next);
    }
}
