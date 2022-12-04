using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetCoreRpc.Core.RpcBuilder
{
    public class AspectPiplineBuilder
    {
        private readonly IList<Func<RpcRequestDelegate, RpcRequestDelegate>> _components ;

        public AspectPiplineBuilder()
        {
            _components = new List<Func<RpcRequestDelegate, RpcRequestDelegate>>();
        }

        public AspectPiplineBuilder Use(Func<RpcContext, RpcRequestDelegate, Task> middleware)
        {
            _components.Add(next => context => middleware(context, next));
            return this;
        }

        public RpcRequestDelegate Build(RpcRequestDelegate _complete)
        {
            var invoke = _complete;
            foreach (var component in _components.Reverse())
            {
                invoke = component(invoke);
            }
            return invoke;
        }
    }
}
