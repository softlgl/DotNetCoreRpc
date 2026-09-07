using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using DotNetCoreRpc.Core;
using DotNetCoreRpc.Server.RpcBuilder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCoreRpc.Server
{
    public class DotNetCoreRpcMiddleware
    {
        private readonly RpcServerOptions _rpcServerOptions;
        private readonly IServiceProvider _serviceProvider;

        // 按 (TypeFullName, MethodName) 元组缓存方法元数据与编译好的执行管道，避免每次请求字符串插值分配
        private readonly ConcurrentDictionary<(string TypeFullName, string MethodName), MethodEntry> _methodCache = new ConcurrentDictionary<(string, string), MethodEntry>();

        // 方法未找到的占位符：ConcurrentDictionary 不允许 null value，用此实例标记"已查过但未找到"，
        // 避免每次未找到方法的请求都重新跑反射工厂
        private static readonly MethodEntry MethodNotFound = new MethodEntry();

        public DotNetCoreRpcMiddleware(RequestDelegate _, RpcServerOptions rpcServerOptions, IServiceProvider serviceProvider)
        {
            _rpcServerOptions = rpcServerOptions;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            RequestModel requestModel = await context.Request.Body.FromMessagePackStream<RequestModel>();
            if (requestModel == null)
            {
                await WriteErrorResponse(context, "读取请求数据失败");
                return;
            }

            await HandleRequest(context, requestModel);
        }

        /// <summary>
        /// 处理请求：解析并缓存方法元数据与管道，按当前请求作用域执行
        /// </summary>
        private async Task HandleRequest(HttpContext context, RequestModel requestModel)
        {
            var methodKey = (requestModel.TypeFullName, requestModel.MethodName);

            MethodEntry methodEntry;
            try
            {
                methodEntry = _methodCache.GetOrAdd(methodKey, _ =>
                {
                    var serviceType = _rpcServerOptions.GetServiceType(requestModel.TypeFullName);
                    // 仅用于发现方法元数据，实例在管道中按当前作用域重新解析
                    var instance = context.RequestServices.GetRequiredService(serviceType);
                    var instanceType = instance.GetType();
                    var method = FindMethod(instanceType, requestModel.MethodName, requestModel.Paramters?.Length ?? 0);
                    if (method == null)
                    {
                        return MethodNotFound;
                    }

                    var aspectContext = new RpcContext { Method = method, TargetType = instanceType, HttpContext = context };
                    // 全局过滤器模板用根 provider 创建，避免捕获首个请求的 scope（captive dependency）
                    var filterTemplates = RpcFilterUtils.GetFilterTemplates(aspectContext, _serviceProvider, _rpcServerOptions.GetFilterTypes());
                    var invoker = TaskUtils.InvokeMethod(method);
                    var pipeline = BuildPipeline(serviceType, invoker, filterTemplates);

                    return new MethodEntry
                    {
                        Method = method,
                        Pipeline = pipeline
                    };
                });
            }
            catch (Exception ex)
            {
                await WriteErrorResponse(context, $"处理请求失败:{ex.Message}");
                return;
            }

            if (methodEntry == MethodNotFound)
            {
                await WriteErrorResponse(context, $"未找到方法:{requestModel.MethodName}");
                return;
            }

            var parameters = requestModel.Paramters;
            if (parameters == null)
            {
                parameters = Array.Empty<object>();
            }

            RpcContext rpcContext = new RpcContext
            {
                Parameters = parameters,
                HttpContext = context,
                TargetType = methodEntry.Method.DeclaringType,
                Method = methodEntry.Method
            };

            await methodEntry.Pipeline(rpcContext);
        }

        /// <summary>
        /// 按方法名 + 参数个数查找方法（协议不携带参数签名，无法进一步区分重载）。
        /// 匹配到多个同参数个数的重载时抛出明确异常，而不是底层 AmbiguousMatchException。
        /// </summary>
        private static MethodInfo FindMethod(Type instanceType, string methodName, int parameterCount)
        {
            var candidates = instanceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name == methodName)
                .ToArray();
            if (candidates.Length == 0)
            {
                return null;
            }

            var matched = candidates.Where(m => m.GetParameters().Length == parameterCount).ToArray();
            if (matched.Length == 0)
            {
                return null;
            }

            if (matched.Length > 1)
            {
                throw new AmbiguousMatchException($"方法 {methodName} 存在 {matched.Length} 个参数个数为 {parameterCount} 的重载，无法确定调用目标");
            }

            return matched[0];
        }

        /// <summary>
        /// 构建执行管道（响应包装 -> 过滤器 -> 终结点），过滤器实例在每个请求中按模板新建
        /// </summary>
        private RpcRequestDelegate BuildPipeline(Type serviceType, Func<object, object?[]?, object?> invoker, List<RpcFilterAttribute> filterTemplates)
        {
            AspectPiplineBuilder aspectPipline = new AspectPiplineBuilder();

            // 响应包装中间件
            aspectPipline.Use(async (rpcContext, next) =>
            {
                try
                {
                    await next(rpcContext);
                }
                catch (Exception ex)
                {
                    // 管道内异常转为协议错误响应，客户端可解析 Code/Message，而不是拿到非协议的 500 页面
                    rpcContext.ReturnValue = new ResponseModel
                    {
                        Code = (int)HttpStatusCode.InternalServerError,
                        Message = $"处理请求异常:{ex.Message}"
                    };
                }

                ResponseModel responseModel = new ResponseModel
                {
                    Data = rpcContext.ReturnValue,
                    Code = (int)HttpStatusCode.OK
                };

                if (rpcContext.ReturnValue is ResponseModel returnValue)
                {
                    responseModel = returnValue;
                }

                rpcContext.HttpContext.Response.ContentType = "application/x-msgpack";
                await rpcContext.HttpContext.Response.Body.WriteToMessagePackStream(responseModel);
            });

            if (filterTemplates != null && filterTemplates.Any())
            {
                foreach (var template in filterTemplates)
                {
                    // 捕获不可变的模板，运行时按当前请求作用域创建独立实例，避免跨请求共享
                    aspectPipline.Use((rpcContext, next) =>
                    {
                        var filter = RpcFilterUtils.CreateFilterInstance(rpcContext.HttpContext.RequestServices, template);
                        return filter.InvokeAsync(rpcContext, next);
                    });
                }
            }

            return aspectPipline.Build(PiplineEndPoint(serviceType, invoker));
        }

        /// <summary>
        /// 管道终结点：按当前请求作用域解析实例并调用方法（使用缓存的编译委托），异步返回值仅 await 一次
        /// </summary>
        private static RpcRequestDelegate PiplineEndPoint(Type serviceType, Func<object, object?[]?, object?> invoker)
        {
            return async rpcContext =>
            {
                var instance = rpcContext.HttpContext.RequestServices.GetRequiredService(serviceType);
                var returnValue = invoker.Invoke(instance, rpcContext.Parameters);

                if (returnValue != null)
                {
                    var returnValueType = returnValue.GetType().GetTypeInfo();
                    if (returnValueType.IsAsync())
                    {
                        // 仅 await 一次，避免 .Result 阻塞线程与 ValueTask 二次消费
                        rpcContext.ReturnValue = await TaskUtils.UnwrapAsync(returnValue, returnValueType).ConfigureAwait(false);
                    }
                    else
                    {
                        rpcContext.ReturnValue = returnValue;
                    }
                }
            };
        }

        private static async Task WriteErrorResponse(HttpContext context, string message)
        {
            context.Response.ContentType = "application/x-msgpack";
            ResponseModel responseModel = new ResponseModel { Code = (int)HttpStatusCode.InternalServerError, Message = message };
            await context.Response.Body.WriteToMessagePackStream(responseModel);
        }

        /// <summary>
        /// 每个方法缓存一次的元数据与执行管道
        /// </summary>
        private sealed class MethodEntry
        {
            public MethodInfo Method;
            public RpcRequestDelegate Pipeline;
        }
    }
}
