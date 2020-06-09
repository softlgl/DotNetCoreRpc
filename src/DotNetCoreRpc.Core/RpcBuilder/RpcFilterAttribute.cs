using System;
using System.Threading.Tasks;

namespace DotNetCoreRpc.Core.RpcBuilder
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public abstract class RpcFilterAttribute : Attribute
    {
        public abstract Task InvokeAsync(RpcContext context, RpcRequestDelegate next);
    }
}
