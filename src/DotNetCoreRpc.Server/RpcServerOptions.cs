using System;
using System.Collections.Generic;
using DotNetCoreRpc.Server.RpcBuilder;

namespace DotNetCoreRpc.Server
{
    public class RpcServerOptions
    {
        private readonly IList<Type> _filterTypes = new List<Type>();
        private readonly ServiceTypeCollection _serviceTypes = new ServiceTypeCollection();

        /// <summary>
        /// 注册全局过滤器，过滤器类型需实现 <see cref="IRpcFilter"/> 接口
        /// </summary>
        public RpcServerOptions AddFilter<TFilter>()
        {
            _filterTypes.Add(typeof(TFilter));
            return this;
        }

        internal IEnumerable<Type> GetFilterTypes()
        {
            return _filterTypes;
        }

        internal Type GetServiceType(string serviceName)
        {
            return _serviceTypes[serviceName];
        }
    }
}
