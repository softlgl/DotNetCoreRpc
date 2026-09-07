using Castle.DynamicProxy;
using DotNetCoreRpc.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetCoreRpc.Client
{
    public partial class RequestHandler
    {
        // 核心编译委托按返回类型全局缓存：handler 作为显式参数传入，不捕获任何实例，
        // 因此可安全跨 RequestHandler 实例共享（表达式只编译一次）
        private static readonly ConcurrentDictionary<TypeInfo, Func<RequestHandler, MethodInfo, object[], object>> _taskCoreFuncCache = new ConcurrentDictionary<TypeInfo, Func<RequestHandler, MethodInfo, object[], object>>();
        private static readonly ConcurrentDictionary<TypeInfo, Func<RequestHandler, MethodInfo, object[], object>> _valueTaskCoreFuncCache = new ConcurrentDictionary<TypeInfo, Func<RequestHandler, MethodInfo, object[], object>>();

        // 实例级包装委托：绑定当前 handler（各自的 HttpClient/BaseAddress），
        // 避免原先静态缓存 + Expression.Constant(this) 导致后续实例复用第一个实例的 HttpClient（客户端串台）
        private readonly ConcurrentDictionary<TypeInfo, Func<MethodInfo, object[], object>> _taskFuncCache = new ConcurrentDictionary<TypeInfo, Func<MethodInfo, object[], object>>();
        private readonly ConcurrentDictionary<TypeInfo, Func<MethodInfo, object[], object>> _valueTaskFuncCache = new ConcurrentDictionary<TypeInfo, Func<MethodInfo, object[], object>>();

        private readonly HttpClient _httpClient;
        private readonly string _requestPath;

        public RequestHandler(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // path 对同一 HttpClient 实例稳定，构造时缓存一次，避免每次请求重复计算
            var pathAndQuery = httpClient.BaseAddress?.PathAndQuery;
            _requestPath = string.IsNullOrWhiteSpace(pathAndQuery) || pathAndQuery == "/"
                ? "/DotNetCoreRpc/ServerRequest"
                : "";
        }

        public object SyncResultHandle(MethodInfo methodInfo, params object[] arguments)
        {
            return TaskResultHandle<object>(methodInfo, arguments).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public Task TaskValueTaskWithoutResultHandle(MethodInfo methodInfo, params object[] arguments)
        {
            return TaskResultHandle<object>(methodInfo, arguments);
        }

        public Func<MethodInfo, object[], object> GetTaskResultHandleFunc(TypeInfo methodReturnType)
        {
            return GetBoundedHandleFunc(_taskFuncCache, _taskCoreFuncCache, nameof(TaskResultHandle), methodReturnType);
        }

        public Func<MethodInfo, object[], object> GetValueResultHandleFunc(TypeInfo methodReturnType)
        {
            return GetBoundedHandleFunc(_valueTaskFuncCache, _valueTaskCoreFuncCache, nameof(ValueResultHandle), methodReturnType);
        }

        // 两个 public 入口共用：先按返回类型取跨实例共享的 Core 委托，再绑定当前 handler
        private Func<MethodInfo, object[], object> GetBoundedHandleFunc(
            ConcurrentDictionary<TypeInfo, Func<MethodInfo, object[], object>> instanceCache,
            ConcurrentDictionary<TypeInfo, Func<RequestHandler, MethodInfo, object[], object>> coreCache,
            string handleMethodName,
            TypeInfo methodReturnType)
        {
            return instanceCache.GetOrAdd(methodReturnType, type => {
                var coreFunc = coreCache.GetOrAdd(type, t => GetHandleFunc(handleMethodName, t));
                return (methodInfo, arguments) => coreFunc(this, methodInfo, arguments);
            });
        }

        private static Func<RequestHandler, MethodInfo, object[], object> GetHandleFunc(string handleMethodName, TypeInfo methodReturnType)
        {
            var returnType = methodReturnType.GetGenericArguments()[0];
            var resultMethod = typeof(RequestHandler).GetMethod(handleMethodName, BindingFlags.NonPublic | BindingFlags.Instance)!.MakeGenericMethod(returnType);
            ParameterExpression handlerSource = Expression.Parameter(typeof(RequestHandler), "handler");
            ParameterExpression methodInfoSource = Expression.Parameter(typeof(MethodInfo), "methodInfo");
            ParameterExpression argumentsSource = Expression.Parameter(typeof(object[]), "arguments");
            var callExpr = Expression.Call(handlerSource, resultMethod, methodInfoSource, argumentsSource);
            var convertBody = Expression.Convert(callExpr, typeof(object));
            var expr = Expression.Lambda<Func<RequestHandler, MethodInfo, object[], object>>(convertBody, handlerSource, methodInfoSource, argumentsSource).Compile();
            return expr;
        }

        private async Task<T> TaskResultHandle<T>(MethodInfo methodInfo, params object[] arguments)
        {
            var result = await SendRequest(methodInfo, arguments).ConfigureAwait(false);
            if (result != null && result.Length != 0)
            {
                ResponseModel responseModel = result.FromMessagePack<ResponseModel>();
                if (responseModel.Code != (int)HttpStatusCode.OK)
                {
                    throw new Exception($"请求出错,返回内容:{responseModel.Message}");
                }

                // MessagePack 响应已按负载里的类型标记还原 Data，无需再经 JsonElement 二次转换
                return (T)responseModel.Data;
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

            // 流式序列化的 HttpContent，避免先 ToMessagePackBytes 产生整块中间字节数组
            HttpContent httpContent = new MessagePackRequestContent(requestModel);

            var responseMessage = await _httpClient.PostAsync(_requestPath, httpContent).ConfigureAwait(false);
            //判断http请求状态
            responseMessage.EnsureSuccessStatusCode();

            byte[] result = await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            return result;
        }
    }
}
