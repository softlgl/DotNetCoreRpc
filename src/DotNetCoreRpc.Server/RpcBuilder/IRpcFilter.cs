using System.Threading.Tasks;

namespace DotNetCoreRpc.Server.RpcBuilder
{
    /// <summary>
    /// RPC 过滤器接口。实现类无需继承 Attribute，可直接用构造函数注入依赖，
    /// 并通过 <c>[RpcFilter(typeof(...))]</c> 挂到服务类或方法上，
    /// 或通过 <c>RpcServerOptions.AddFilter&lt;T&gt;()</c> 注册为全局过滤器。
    /// </summary>
    public interface IRpcFilter
    {
        Task InvokeAsync(RpcContext context, RpcRequestDelegate next);
    }
}
