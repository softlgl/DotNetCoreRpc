using System;
using System.Collections.Concurrent;
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
        private readonly IDictionary<string,Type> _types;
        private readonly IEnumerable<Type> _filterTypes;
        private readonly ConcurrentDictionary<string, List<RpcFilterAttribute>> _methodFilters = new ConcurrentDictionary<string, List<RpcFilterAttribute>>();

        public DotNetCoreRpcMiddleware(RequestDelegate next, RpcServerOptions rpcServerOptions)
        {
            _types = rpcServerOptions.GetTypes();
            _filterTypes = rpcServerOptions.GetFilterTypes();
        }

        public async Task InvokeAsync(HttpContext context)
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

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <returns></returns>
        private async Task HandleRequest(HttpContext context, ResponseModel responseModel, RequestModel requestModel)
        {
            Type serviceType = _types[requestModel.TypeFullName];
            var instance = context.RequestServices.GetService(serviceType);
            var instanceType = instance.GetType();
            var method = instanceType.GetMethod(requestModel.MethodName);
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
                Parameters = paramters,
                HttpContext = context,
                TargetType = instanceType,
                Method = method
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
            List<RpcFilterAttribute> interceptorAttributes = GetFilterAttributes(context, method);
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
        private List<RpcFilterAttribute> GetFilterAttributes(HttpContext context,MethodInfo method)
        {
            var methondInterceptorAttributes = _methodFilters.GetOrAdd($"{method.DeclaringType.FullName}#{method.Name}",
                key=>{
                    var methondAttributes = method.GetCustomAttributes(true)
                                   .Where(i => typeof(RpcFilterAttribute).IsAssignableFrom(i.GetType()))
                                   .Cast<RpcFilterAttribute>().ToList();
                    var classAttributes = method.DeclaringType.GetCustomAttributes(true)
                        .Where(i => typeof(RpcFilterAttribute).IsAssignableFrom(i.GetType()))
                        .Cast<RpcFilterAttribute>();
                    methondAttributes.AddRange(classAttributes);
                    var glableInterceptorAttribute = RpcFilterUtils.GetInstances(context.RequestServices, _filterTypes);
                    methondAttributes.AddRange(glableInterceptorAttribute);
                    return methondAttributes;
                });
            RpcFilterUtils.PropertiesInject(context.RequestServices, methondInterceptorAttributes);
            return methondInterceptorAttributes;
        }
    }
}
