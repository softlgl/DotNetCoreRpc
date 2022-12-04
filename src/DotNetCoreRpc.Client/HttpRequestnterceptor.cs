using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using DotNetCoreRpc.Core;

namespace DotNetCoreRpc.Client
{
    public class HttpRequestInterceptor : IInterceptor
    {
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
                var methodReturnType = methodInfo.ReturnType.GetTypeInfo();
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
                        if (!methodReturnType.IsAsync())
                        {
                            invocation.ReturnValue = responseModel.Data.ToJson().FromJson(methodInfo.ReturnType);
                            return;
                        }

                        var returnValue = responseModel.Data.ToJson().FromJson(methodReturnType.GetGenericArguments()[0]);
                        var resultType = invocation.Method.ReturnType.GetGenericArguments()[0];
                        if (methodReturnType.IsTaskWithResult())
                        {
                            invocation.ReturnValue = TaskUtils.TaskResultFunc(resultType).Invoke(returnValue);
                            return;
                        }

                        if (methodReturnType.IsValueTaskWithResult())
                        {
                            invocation.ReturnValue = TaskUtils.ValueTaskResultFunc(resultType).Invoke(returnValue);   
                            return;
                        }    
                    }

                    if (methodReturnType.IsTask() || methodReturnType.IsValueTask())
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
