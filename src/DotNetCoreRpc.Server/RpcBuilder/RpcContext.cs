using System;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace DotNetCoreRpc.Server.RpcBuilder
{
    public class RpcContext
    {
        public object ReturnValue { get; set; } 

        public object[] Parameters { get; set; }

        public Type TargetType { get; set; }

        public MethodInfo Method { get; set; }

        public HttpContext HttpContext { get; set; }
    }
}
