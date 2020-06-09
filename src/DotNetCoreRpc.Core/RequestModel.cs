using System;
namespace DotNetCoreRpc.Core
{
    public class RequestModel
    {
        public string TypeFullName { get; set; }
        public string MethodName { get; set; }
        public object[] Paramters { get; set; }
    }
}
