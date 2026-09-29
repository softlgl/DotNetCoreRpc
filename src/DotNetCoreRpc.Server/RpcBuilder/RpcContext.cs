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

        /// <summary>
        /// 当前请求作用域的 IServiceProvider，供过滤器在管道中按需解析服务
        /// </summary>
        public IServiceProvider ServiceProvider { get; set; }
    }
}
