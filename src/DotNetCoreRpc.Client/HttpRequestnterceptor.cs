using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
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
            HandleRequest(invocation).GetAwaiter().GetResult();
        }

        private async Task HandleRequest(IInvocation invocation)
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
            var responseMessage = await _httpClient.PostAsync("/DotNetCoreRpc/ServerRequest", httpContent);
            if (responseMessage.StatusCode == HttpStatusCode.OK)
            {
                var methodReturnType = methodInfo.ReturnType.GetTypeInfo();
                byte[] result = await responseMessage.Content.ReadAsByteArrayAsync();
                if (result != null && result.Length != 0)
                {
                    ResponseModel responseModel = result.FromJson<ResponseModel>();
                    if (responseModel.Code != (int)HttpStatusCode.OK)
                    {
                        throw new Exception($"请求出错,返回内容:{Encoding.UTF8.GetString(result)}");
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
