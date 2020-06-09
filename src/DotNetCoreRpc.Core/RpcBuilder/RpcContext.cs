using System;
using System.Reflection;
using Microsoft.AspNetCore.Http;

namespace DotNetCoreRpc.Core.RpcBuilder
{
    public class RpcContext
    {
        public object ReturnValue { get; set; } 

        public object[] Parameters { get; set; }

        public HttpContext HttpContext { get; set; }
    }
}
