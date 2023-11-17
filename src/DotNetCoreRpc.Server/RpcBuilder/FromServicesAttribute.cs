using System;

namespace DotNetCoreRpc.Server.RpcBuilder
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FromServicesAttribute : Attribute
    {
        public FromServicesAttribute() { }

#if NET8_0_OR_GREATER

        private string _serviceKey;

        public FromServicesAttribute(string serviceKey)
        {
            _serviceKey = serviceKey;
        }
        public string SeviceKey => _serviceKey;

#endif
    }
}
