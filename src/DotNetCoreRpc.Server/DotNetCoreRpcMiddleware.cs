using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using DotNetCoreRpc.Core;
using DotNetCoreRpc.Core.RpcBuilder;
using Microsoft.AspNetCore.Http;

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
            //context.Request.EnableBuffering();
            //context.Request.Body.Seek(0, SeekOrigin.Begin);
            //var requestReader = new StreamReader(context.Request.Body);
            //var requestContent = await requestReader.ReadToEndAsync();
            //context.Request.Body.Seek(0, SeekOrigin.Begin);

            var requestContent = await context.Request.ReadStringAsync();
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
            await HandleRequest(context, requestModel);
            return;
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <returns></returns>
        private async Task HandleRequest(HttpContext context, RequestModel requestModel)
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
                ResponseModel responseModel = new ResponseModel
                {
                    Data = rpcContext.ReturnValue,
                    Code = 200
                };
                await aspectContext.HttpContext.Response.WriteAsync(responseModel.ToJson());
            });
            List<RpcFilterAttribute> interceptorAttributes = GetFilterAttributes(aspectContext);
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
                var targetParameter = Expression.Parameter(typeof(object), "target");
                var parametersParameter = Expression.Parameter(typeof(object?[]), "parameters");
                var paramInfos = aspectContext.Method.GetParameters();
                var parameters = new List<Expression>(paramInfos.Length);
                for (int i = 0; i < paramInfos.Length; i++)
                {
                    var paramInfo = paramInfos[i];
                    var valueObj = Expression.ArrayIndex(parametersParameter, Expression.Constant(i));
                    var valueCast = Expression.Convert(valueObj, paramInfo.ParameterType);

                    // valueCast is "(Ti) parameters[i]"
                    parameters.Add(valueCast);
                }
                var instanceCast = Expression.Convert(targetParameter, aspectContext.TargetType.GetTypeInfo().AsType());
                var methodCall = Expression.Call(instanceCast, aspectContext.Method, parameters);

                Func<object, object?[]?, object?> func = null;
                if (methodCall.Type == typeof(void))
                {
                    var lambda = Expression.Lambda<Action<object, object?[]?>>(methodCall, targetParameter, parametersParameter);
                    func = (target, parameters) =>
                    {
                        lambda.Compile().Invoke(target, parameters);
                        return null;
                    };
                }
                else
                {
                    var castMethodCall = Expression.Convert(methodCall, typeof(object));
                    var lambda = Expression.Lambda<Func<object, object?[]?, object?>>(castMethodCall, targetParameter, parametersParameter);
                    func = lambda.Compile();
                }
                var returnValue = func.Invoke(instance, aspectContext.Parameters);
                if (returnValue != null)
                {
                    var returnValueType = returnValue.GetType().GetTypeInfo();
                    if (returnValueType.IsAsync())
                    {
                        if (returnValueType.IsTask() || returnValueType.IsValueTask())
                        {
                            return;
                        }

                        if (returnValueType.IsTaskWithVoidTaskResult())
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
                        //}
                        //if (returnValue is Task || returnValue is ValueTask)
                        //{
                        //    return;
                        //}
                    }
                    aspectContext.ReturnValue = returnValue;
                }
                return;
            };
        }

        /// <summary>
        /// 获取Attribute
        /// </summary>
        /// <returns></returns>
        private List<RpcFilterAttribute> GetFilterAttributes(RpcContext aspectContext)
        {
            var methondInfo = aspectContext.Method;
            var methondInterceptorAttributes = _methodFilters.GetOrAdd($"{methondInfo.DeclaringType.FullName}#{methondInfo.Name}",
                key=>{
                    var methondAttributes = methondInfo.GetCustomAttributes(true)
                                   .Where(i => typeof(RpcFilterAttribute).IsAssignableFrom(i.GetType()))
                                   .Cast<RpcFilterAttribute>().ToList();
                    var classAttributes = methondInfo.DeclaringType.GetCustomAttributes(true)
                        .Where(i => typeof(RpcFilterAttribute).IsAssignableFrom(i.GetType()))
                        .Cast<RpcFilterAttribute>();
                    methondAttributes.AddRange(classAttributes);
                    var glableInterceptorAttribute = RpcFilterUtils.GetInstances(aspectContext.HttpContext.RequestServices, _filterTypes);
                    methondAttributes.AddRange(glableInterceptorAttribute);
                    return methondAttributes;
                });
            RpcFilterUtils.PropertiesInject(aspectContext.HttpContext.RequestServices, methondInterceptorAttributes);
            return methondInterceptorAttributes;
        }
    }
}
