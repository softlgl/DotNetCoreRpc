using System;
namespace DotNetCoreRpc.Core
{
    public class RequestModel
    {
        public string TypeFullName { get; set; }
        public string MethodName { get; set; }
        public object[] Parameters { get; set; }
    }
}
