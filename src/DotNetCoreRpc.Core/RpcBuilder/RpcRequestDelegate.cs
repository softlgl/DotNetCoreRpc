using System;
using System.Threading.Tasks;

namespace DotNetCoreRpc.Core.RpcBuilder
{
    public delegate Task RpcRequestDelegate(RpcContext context);
}
