using Castle.DynamicProxy;
using DotNetCoreRpc.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCoreRpc.Client
{
    public class RequestHandler
    {
        private static readonly ConcurrentDictionary<TypeInfo, Func<MethodInfo, object[], object>> _taskFuncCache = new ConcurrentDictionary<TypeInfo, Func<MethodInfo, object[], object>>();
        private static readonly ConcurrentDictionary<TypeInfo, Func<MethodInfo, object[], object>> _valueTaskFuncCache = new ConcurrentDictionary<TypeInfo, Func<MethodInfo, object[], object>>();

        private readonly HttpClient _httpClient;
        public RequestHandler(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public object SyncResultHandle(MethodInfo methodInfo, params object[] arguments)
        {
            return TaskResultHandle<object>(methodInfo, arguments).GetAwaiter().GetResult();
        }

        public Task TaskValueTaskWithoutResultHandle(MethodInfo methodInfo, params object[] arguments)
        {
            return TaskResultHandle<object>(methodInfo, arguments);
        }

        public Func<MethodInfo, object[], object> GetTaskResultHandleFunc(TypeInfo methodReturnType)
        {
            return _taskFuncCache.GetOrAdd(methodReturnType, type => {
                return GetHandleFunc(nameof(TaskResultHandle), type);
            });
        }

        public Func<MethodInfo, object[], object> GetValueResultHandleFunc(TypeInfo methodReturnType)
        {
            return _valueTaskFuncCache.GetOrAdd(methodReturnType, type => {
                return GetHandleFunc(nameof(ValueResultHandle), type);
            });
        }

        private Func<MethodInfo, object[], object> GetHandleFunc(string handleMethodName, TypeInfo methodReturnType)
        {
            var returnType = methodReturnType.GetGenericArguments()[0];
            var resultMethod = GetType().GetMethod(handleMethodName, BindingFlags.NonPublic | BindingFlags.Instance)!.MakeGenericMethod(returnType);
            ParameterExpression methodInfoSource = Expression.Parameter(typeof(MethodInfo), "methodInfo");
            ParameterExpression argumentsSource = Expression.Parameter(typeof(object[]), "arguments");
            var instance = Expression.Constant(this);
            var callExpr = Expression.Call(instance, resultMethod, methodInfoSource, argumentsSource);
            var convertBody = Expression.Convert(callExpr, typeof(object));
            var expr = Expression.Lambda<Func<MethodInfo, object[], object>>(convertBody, methodInfoSource, argumentsSource).Compile();
            return expr;
        }

        private async Task<T> TaskResultHandle<T>(MethodInfo methodInfo, params object[] arguments)
        {
            var result = await SendRequest(methodInfo, arguments);
            if (result != null && result.Length != 0)
            {
                ResponseModel responseModel = result.FromJson<ResponseModel>();
                if (responseModel.Code != (int)HttpStatusCode.OK)
                {
                    throw new Exception($"请求出错,返回内容:{responseModel.Message}");
                }

                TypeInfo methodReturnType = methodInfo.ReturnType.GetTypeInfo();
                if (methodReturnType.IsAsync() && (methodReturnType.IsTaskWithResult() || methodReturnType.IsValueTaskWithResult()))
                {
                    methodReturnType = methodReturnType.GetGenericArguments()[0].GetTypeInfo();
                }
               
                if (responseModel.Data is JsonElement jsonElement)
                {
                    var returnValue = jsonElement.FromJson(methodReturnType);
                    return (T)returnValue;
                }
            }

            return default;
        }

        private ValueTask<T> ValueResultHandle<T>(MethodInfo methodInfo, params object[] arguments)
        {
            var taskResult = TaskResultHandle<T>(methodInfo, arguments);
            return new ValueTask<T>(taskResult);
        }

        private async Task<byte[]> SendRequest(MethodInfo methodInfo, params object[] arguments)
        {
            var requestModel = new RequestModel
            {
                TypeFullName = methodInfo.DeclaringType.FullName,
                MethodName = methodInfo.Name,
                Paramters = arguments
            };

            HttpContent httpContent = new StringContent(requestModel.ToJson());
            httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            string path = string.IsNullOrWhiteSpace(_httpClient.BaseAddress.PathAndQuery) || _httpClient.BaseAddress.PathAndQuery == "/" 
                ? "/DotNetCoreRpc/ServerRequest" : "";

            var responseMessage = await _httpClient.PostAsync(path, httpContent);
            byte[] result = await responseMessage.Content.ReadAsByteArrayAsync();
            //判断http请求状态
            responseMessage.EnsureSuccessStatusCode();
            return result;
        }
    }
}
