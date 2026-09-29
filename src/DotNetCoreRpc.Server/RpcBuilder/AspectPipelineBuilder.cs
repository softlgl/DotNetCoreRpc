using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCoreRpc.Server.RpcBuilder
{
    public class AspectPipelineBuilder
    {
        private readonly IList<Func<RpcRequestDelegate, RpcRequestDelegate>> _components;

        public AspectPipelineBuilder()
        {
            _components = new List<Func<RpcRequestDelegate, RpcRequestDelegate>>();
        }

        public AspectPipelineBuilder Use(Func<RpcContext, RpcRequestDelegate, Task> middleware)
        {
            if (middleware == null)
            {
                throw new ArgumentNullException(nameof(middleware));
            }

            _components.Add(next => context => middleware(context, next));
            return this;
        }

        public RpcRequestDelegate Build(RpcRequestDelegate complete)
        {
            if (complete == null)
            {
                throw new ArgumentNullException(nameof(complete));
            }

            var invoke = complete;
            foreach (var component in _components.Reverse())
            {
                invoke = component(invoke);
            }
            return invoke;
        }
    }
}
