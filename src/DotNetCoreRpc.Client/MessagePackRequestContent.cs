using DotNetCoreRpc.Core;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DotNetCoreRpc.Client
{
    /// <summary>
    /// 请求体流式序列化：发送时直接把模型序列化进请求流，跳过中间 byte[] 拷贝。
    /// 序列化可重复执行，HttpClient 连接重试/重定向时再次调用也安全。
    /// </summary>
    public sealed class MessagePackRequestContent : HttpContent
    {
        private readonly RequestModel _requestModel;

        public MessagePackRequestContent(RequestModel requestModel)
        {
            if (requestModel == null)
            {
                throw new ArgumentNullException(nameof(requestModel));
            }

            _requestModel = requestModel;
            Headers.ContentType = new MediaTypeHeaderValue("application/x-msgpack");
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            await stream.WriteToMessagePackStream(_requestModel).ConfigureAwait(false);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
