using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using DotNetCoreRpc.Core;

namespace DotNetCoreRpc.Client
{
    public class HttpRequestInterceptor : IInterceptor
    {
        private readonly RequestHandler _requestHandler;
        public HttpRequestInterceptor(HttpClient httpClient)
        {
            _requestHandler = new RequestHandler(httpClient);
        }

        public void Intercept(IInvocation invocation)
        {
            var methodReturnType = invocation.Method.ReturnType.GetTypeInfo();

            if (!methodReturnType.IsAsync())
            {
                var result = _requestHandler.SyncResultHandle(invocation.Method, invocation.Arguments);
                if (result == null)
                {
                    return;
                }

                invocation.ReturnValue = result;
                return;
            }

            if (methodReturnType.IsTask())
            {
                invocation.ReturnValue = _requestHandler.TaskValueTaskWithoutResultHandle(invocation.Method, invocation.Arguments);
                return;
            }

            if (methodReturnType.IsValueTask())
            {
                invocation.ReturnValue = new ValueTask(_requestHandler.TaskValueTaskWithoutResultHandle(invocation.Method, invocation.Arguments));
                return;
            }

            if (methodReturnType.IsTaskWithResult())
            {
                invocation.ReturnValue = _requestHandler.GetTaskResultHandleFunc(methodReturnType).Invoke(invocation.Method, invocation.Arguments);
                return;
            }

            if (methodReturnType.IsValueTaskWithResult())
            {
                invocation.ReturnValue = _requestHandler.GetValueResultHandleFunc(methodReturnType).Invoke(invocation.Method, invocation.Arguments);
                return;
            }
         }

    }
}
