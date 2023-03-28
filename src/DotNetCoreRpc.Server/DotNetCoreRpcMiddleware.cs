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
using DotNetCoreRpc.Core.RpcBuilder;
using Microsoft.AspNetCore.Http;

namespace DotNetCoreRpc.Server
{
    public class DotNetCoreRpcMiddleware
    {
        private readonly IDictionary<string, Type> _serviceTypes;
        private readonly IEnumerable<Type> _filterTypes;

        public DotNetCoreRpcMiddleware(RequestDelegate next, RpcServerOptions rpcServerOptions)
        {
            _serviceTypes = rpcServerOptions.GetRegisterTypes();
            _filterTypes = rpcServerOptions.GetFilterTypes();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var requestContent = await context.Request.ReadStringAsync();
            ResponseModel responseModel = new ResponseModel{ Code = (int)HttpStatusCode.InternalServerError };
            if (string.IsNullOrEmpty(requestContent))
            {
                responseModel.Message = "未读取到请求信息";
                await context.Response.WriteAsync(responseModel.ToJson());
                return;
            }

            RequestModel requestModel = requestContent.FromJson<RequestModel>();
            if (requestModel == null)
            {
                responseModel.Message = "读取请求数据失败";
                await context.Response.WriteAsync(responseModel.ToJson());
                return;
            }

            if (!_serviceTypes.ContainsKey(requestModel.TypeFullName))
            {
                responseModel.Message = $"{requestModel.TypeFullName}未注册";
                await context.Response.WriteAsync(responseModel.ToJson());
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
            Type serviceType = _serviceTypes[requestModel.TypeFullName];
            var instance = context.RequestServices.GetService(serviceType);
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
                await aspectContext.HttpContext.Response.WriteAsync(responseModel.ToJson());
            });

            List<RpcFilterAttribute> interceptorAttributes = RpcFilterUtils.GetFilterAttributes(aspectContext, _filterTypes);
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
