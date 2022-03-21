using System;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using DotNetCoreRpc.Core;

namespace DotNetCoreRpc.Client
{
    public class HttpRequestInterceptor : IInterceptor
    {
        private readonly MethodInfo commonMethodInfo = typeof(HttpRequestInterceptor).GetMethod("HandleAsync", BindingFlags.Instance | BindingFlags.Public);
        private readonly MethodInfo commonValueTaskMethodInfo = typeof(HttpRequestInterceptor).GetMethod("HandleValueTaskAsync", BindingFlags.Instance | BindingFlags.Public);

        private readonly HttpClient _httpClient;
        public HttpRequestInterceptor(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public void Intercept(IInvocation invocation)
        {
            var methodInfo = invocation.Method;
            var requestModel = new RequestModel
            {
                TypeFullName = methodInfo.DeclaringType.FullName,
                MethodName = methodInfo.Name,
                Paramters = invocation.Arguments
            };
            HttpContent httpContent = new StringContent(requestModel.ToJson());
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var responseMessage = _httpClient.PostAsync("/DotNetCoreRpc/ServerRequest", httpContent).GetAwaiter().GetResult();
            if (responseMessage.StatusCode == HttpStatusCode.OK)
            {
                var methodReturnType = methodInfo.ReturnType;
                string result = responseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(result))
                {
                    ResponseModel responseModel = result.FromJson<ResponseModel>();
                    if (responseModel.Code != (int)HttpStatusCode.OK)
                    {
                        throw new Exception($"请求出错,返回内容:{result}");
                    }
                    if (responseModel.Data != null)
                    {
                        if (IsAsyncMethod(methodInfo))
                        {
                            if (methodReturnType.IsGenericType)
                            {
                                var returnValue = responseModel.Data.ToJson().FromJson(methodReturnType.GetGenericArguments()[0]);
                                var resultType = invocation.Method.ReturnType.GetGenericArguments()[0];
                                if (methodReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                                {
                                    var mi = commonMethodInfo.MakeGenericMethod(resultType);
                                    invocation.ReturnValue = mi.Invoke(this, new[] { Task.FromResult(returnValue) });
                                }
                                if (methodReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                                {
                                    var mi = commonValueTaskMethodInfo.MakeGenericMethod(resultType);
                                    object vTask = new ValueTask<object>(returnValue);
                                    invocation.ReturnValue = mi.Invoke(this, new [] { vTask });
                                }
                                return;
                            }
                        }
                        invocation.ReturnValue = responseModel.Data.ToJson().FromJson(methodInfo.ReturnType);
                        return;
                    }
                    if (methodReturnType == typeof(Task) || methodReturnType == typeof(ValueTask))
                    {
                        invocation.ReturnValue = Task.CompletedTask;
                        return;
                    }
                }
                return;
            }
            throw new Exception($"请求异常,StatusCode:{responseMessage.StatusCode}");
        }

        private bool IsAsyncMethod(MethodInfo method)
        {
            bool isDefAsync = Attribute.IsDefined(method, typeof(AsyncStateMachineAttribute), false);
            bool isTaskType = CheckMethodReturnTypeIsTaskType(method);
            bool isAsync = isDefAsync || isTaskType;
            return isAsync;
        }

        private bool CheckMethodReturnTypeIsTaskType(MethodInfo method)
        {
            var methodReturnType = method.ReturnType;
            if (methodReturnType.IsGenericType)
            {
                if (methodReturnType.GetGenericTypeDefinition() == typeof(Task<>) ||
                    methodReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                    return true;
            }
            else
            {
                if (methodReturnType == typeof(Task) ||
                    methodReturnType == typeof(ValueTask))
                    return true;
            }
            return false;
        }

        //构造等待返回值的异步方法
        public async Task<T> HandleAsync<T>(Task<object> task)
        {
            var t = await task;
            return (T)t;
        }

        //构造等待返回值的异步方法
        public async ValueTask<T> HandleValueTaskAsync<T>(ValueTask<object> task)
        {
            var t = await task;
            return (T)t;
        }
    }
}
