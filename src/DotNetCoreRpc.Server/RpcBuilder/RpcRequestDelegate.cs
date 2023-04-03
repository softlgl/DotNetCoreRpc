using System;
using System.Threading.Tasks;

namespace DotNetCoreRpc.Server.RpcBuilder
{
    public delegate Task RpcRequestDelegate(RpcContext context);
}
