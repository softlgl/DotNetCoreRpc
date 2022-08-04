using System;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<Type, MethodInfo> _taskTypeCache = new ConcurrentDictionary<Type, MethodInfo>();
        private static readonly ConcurrentDictionary<Type, Type> _valueTaskCache = new ConcurrentDictionary<Type, Type>();

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
                        if (TaskUtils.IsAsyncMethod(methodInfo))
                        {
                            if (methodReturnType.IsGenericType)
                            {
                                var returnValue = responseModel.Data.ToJson().FromJson(methodReturnType.GetGenericArguments()[0]);
                                var resultType = invocation.Method.ReturnType.GetGenericArguments()[0];
                                if (methodReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                                {
                                    var mi = _taskTypeCache.GetOrAdd(resultType, type=> typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType));
                                    invocation.ReturnValue = mi.Invoke(this, new[] { returnValue });
                                }
                                if (methodReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                                {
                                    invocation.ReturnValue = Activator.CreateInstance(_valueTaskCache.GetOrAdd(resultType, type => typeof(ValueTask<>).MakeGenericType(resultType)), returnValue);
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
    }
}
