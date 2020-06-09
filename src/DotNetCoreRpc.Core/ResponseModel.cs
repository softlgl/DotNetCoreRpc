using System;
namespace DotNetCoreRpc.Core
{
    public class ResponseModel
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}
