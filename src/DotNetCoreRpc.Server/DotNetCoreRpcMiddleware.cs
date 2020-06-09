﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DotNetCoreRpc.Core;
using DotNetCoreRpc.Core.RpcBuilder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace DotNetCoreRpc.Server
{
    public class DotNetCoreRpcMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDictionary<string,Type> _types;
        public DotNetCoreRpcMiddleware(RequestDelegate next, RpcServerOptions rpcServerOptions)
        {
            _next = next;
            _types = rpcServerOptions.GetTypes();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey("User-Agent")
                && context.Request.Headers["User-Agent"]=="DotNetCoreRpc.Client")
            {
                var syncIOFeature = context.Features.Get<IHttpBodyControlFeature>();
                if (syncIOFeature != null)
                {
                    syncIOFeature.AllowSynchronousIO = true;
                }
                var requestReader = new StreamReader(context.Request.Body);
                var requestContent = requestReader.ReadToEnd();
                ResponseModel responseModel = new ResponseModel
                {
                    Code = 500
                };
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
                if (!_types.ContainsKey(requestModel.TypeFullName))
                {
                    responseModel.Message = $"{requestModel.TypeFullName}未注册";
                    await context.Response.WriteAsync(responseModel.ToJson());
                    return;
                }
                await HandleRequest(context, responseModel, requestModel);
                return;
            }
            await _next(context);
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <returns></returns>
        private async Task HandleRequest(HttpContext context, ResponseModel responseModel, RequestModel requestModel)
        {
            Type serviceType = _types[requestModel.TypeFullName];
            var instance = context.RequestServices.GetService(serviceType);
            var method = instance.GetType().GetMethod(requestModel.MethodName);
            var methodParamters = method.GetParameters();
            var paramters = requestModel.Paramters;
            for (int i = 0; i < paramters.Length; i++)
            {
                if (paramters[i].GetType() != methodParamters[i].ParameterType)
                {
                    paramters[i] = paramters[i].ToJson().FromJson(methodParamters[i].ParameterType);
                }
            }
            AspectPiplineBuilder aspectPipline = CreatPipleline(context, responseModel, method);
            RpcContext aspectContext = new RpcContext
            {
                Parameters = requestModel.Paramters,
                HttpContext = context,
            };
            RpcRequestDelegate rpcRequestDelegate = aspectPipline.Build(PiplineEndPoint(requestModel, instance, aspectContext));
            await rpcRequestDelegate(aspectContext);
        }

        /// <summary>
        /// 创建执行管道
        /// </summary>
        /// <param name="context"></param>
        /// <param name="responseModel"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        private AspectPiplineBuilder CreatPipleline(HttpContext context, ResponseModel responseModel, MethodInfo method)
        {
            AspectPiplineBuilder aspectPipline = new AspectPiplineBuilder();
            //第一个中间件构建包装数据
            aspectPipline.Use(async (rpcContext, next) =>
            {
                await next(rpcContext);
                responseModel.Data = rpcContext.ReturnValue;
                responseModel.Code = 200;
                await context.Response.WriteAsync(responseModel.ToJson());
            });
            List<RpcFilterAttribute> interceptorAttributes = GetFilterAttributes(method);
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
        private static RpcRequestDelegate PiplineEndPoint(RequestModel requestModel, object instance, RpcContext aspectContext)
        {
            return rpcContext =>
            {
                var returnValue = instance.GetType().GetMethod(requestModel.MethodName).Invoke(instance, requestModel.Paramters);
                if (returnValue != null)
                {
                    var returnValueType = returnValue.GetType();
                    if (typeof(Task).IsAssignableFrom(returnValueType))
                    {
                        var resultProperty = returnValueType.GetProperty("Result");
                        aspectContext.ReturnValue = resultProperty.GetValue(returnValue);
                        return Task.CompletedTask;
                    }
                    aspectContext.ReturnValue = returnValue;
                }
                return Task.CompletedTask;
            };
        }

        /// <summary>
        /// 获取Attribute
        /// </summary>
        /// <returns></returns>
        private List<RpcFilterAttribute> GetFilterAttributes(MethodInfo method)
        {
            var classInterceptorAttributes = method.DeclaringType.GetCustomAttributes(true)
                .Where(i => typeof(RpcFilterAttribute).IsAssignableFrom(i.GetType()))
                .Cast<RpcFilterAttribute>().ToList();
            var methondInterceptorAttributes = method.GetCustomAttributes(true)
                .Where(i => typeof(RpcFilterAttribute).IsAssignableFrom(i.GetType()))
                .Cast<RpcFilterAttribute>();
            classInterceptorAttributes.AddRange(methondInterceptorAttributes);
            return classInterceptorAttributes;
        }
    }
}