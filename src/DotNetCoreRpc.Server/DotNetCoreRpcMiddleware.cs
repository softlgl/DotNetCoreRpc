using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text.Json;
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

        public DotNetCoreRpcMiddleware(RequestDelegate _, RpcServerOptions rpcServerOptions, IServiceProvider serviceProvider)
        {
            _rpcServerOptions = rpcServerOptions;
            _serviceProvider = serviceProvider;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Request.EnableBuffering();

            RequestModel requestModel = await context.Request.Body.FromStream<RequestModel>();
            if (requestModel == null)
            {
                ResponseModel responseModel = new ResponseModel { Code = (int)HttpStatusCode.InternalServerError, Message = "读取请求数据失败" };
                await responseModel.WriteToStream(context.Response.Body);
                return;
            }

            await HandleRequest(context, requestModel);
            return;
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <returns></returns>
        private async Task HandleRequest(HttpContext context, RequestModel requestModel)
        {
            var serviceType = _rpcServerOptions.GetServiceType(requestModel.TypeFullName);
            var instance = context.RequestServices.GetRequiredService(serviceType);
            var instanceType = instance.GetType();
            var method = instanceType.GetMethod(requestModel.MethodName);
            var methodParamters = method.GetParameters();
            var paramters = requestModel.Paramters;
            for (int i = 0; i < paramters.Length; i++)
            {
                if (paramters[i].GetType() != methodParamters[i].ParameterType)
                {
                    if (paramters[i] is JsonElement jsonElement)
                    {
                        paramters[i] = jsonElement.FromJson(methodParamters[i].ParameterType);
                    }
                }
            }

            RpcContext aspectContext = new RpcContext
            {
                Parameters = paramters,
                HttpContext = context,
                TargetType = instanceType,
                Method = method
            };
            AspectPiplineBuilder aspectPipline = CreatPipleline(aspectContext);
            RpcRequestDelegate rpcRequestDelegate = aspectPipline.Build(PiplineEndPoint(instance, aspectContext));
            await rpcRequestDelegate(aspectContext);
        }

        /// <summary>
        /// 创建执行管道
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private AspectPiplineBuilder CreatPipleline(RpcContext aspectContext)
        {
            AspectPiplineBuilder aspectPipline = new AspectPiplineBuilder();

            //第一个中间件构建包装数据
            aspectPipline.Use(async (rpcContext, next) =>
            {
                await next(rpcContext);

                //返回结果
                ResponseModel responseModel = new ResponseModel
                {
                    Data = rpcContext.ReturnValue,
                    Code = (int)HttpStatusCode.OK
                };

                if (rpcContext.ReturnValue is ResponseModel returnValue)
                {
                    responseModel = returnValue;
                }

                await responseModel.WriteToStream(rpcContext.HttpContext.Response.Body);
            });

            List<RpcFilterAttribute> interceptorAttributes = RpcFilterUtils.GetFilterAttributes(aspectContext, _serviceProvider, _rpcServerOptions.GetFilterTypes());
            if (interceptorAttributes.Any())
            {
                foreach (var item in interceptorAttributes)
                {
                    aspectPipline.Use(item.InvokeAsync);
                }
            }
            return aspectPipline;
        }

        /// <summary>
        /// 管道终结点
        /// </summary>
        /// <returns></returns>
        private static RpcRequestDelegate PiplineEndPoint(object instance, RpcContext aspectContext)
        {
            return async rpcContext =>
            {
                var func = TaskUtils.InvokeMethod(aspectContext.Method);
                var returnValue = func.Invoke(instance, aspectContext.Parameters);

                if (returnValue != null)
                {
                    var returnValueType = returnValue.GetType().GetTypeInfo();
                    if (returnValueType.IsAsync())
                    {
                        if (returnValueType.IsTask() || returnValueType.IsValueTask() || returnValueType.IsTaskWithVoidTaskResult())
                        {
                            return;
                        }

                        if (returnValueType.IsTaskWithResult())
                        {
                            aspectContext.ReturnValue = TaskUtils.CreateFuncToGetTaskResult(returnValueType).Invoke(returnValue);
                            return;
                        }

                        if (returnValueType.IsValueTaskWithResult())
                        {
                            await TaskUtils.ValueTaskWithResultToTask(returnValue, returnValueType);
                            aspectContext.ReturnValue = TaskUtils.CreateFuncToGetTaskResult(returnValueType).Invoke(returnValue);
                            return;
                        }
                    }
                    aspectContext.ReturnValue = returnValue;
                }
                return;
            };
        }
    }
}
