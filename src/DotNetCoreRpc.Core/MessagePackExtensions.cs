using System;
using System.IO;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Resolvers;

namespace DotNetCoreRpc.Core
{
    /// <summary>
    /// RPC 请求/响应链路的二进制序列化封装（MessagePack）。
    /// 通过 <see cref="TypelessContractlessStandardResolver"/> 在负载中携带类型标记，
    /// 从而在请求方/响应方不一致时仍能还原 object 多态参数（如 RequestModel.Parameters / ResponseModel.Data）。
    /// 与 JsonExtensions 不同，本类只服务于 RPC 协议字节流，不用于面向日志的辅助序列化。
    /// </summary>
    public static class MessagePackExtensions
    {
        private static readonly MessagePackSerializerOptions Options =
            MessagePackSerializerOptions.Standard
                .WithResolver(TypelessContractlessStandardResolver.Instance);

        public static byte[] ToMessagePackBytes<T>(this T data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return MessagePackSerializer.Serialize(data, Options);
        }

        public static T FromMessagePack<T>(this byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return MessagePackSerializer.Deserialize<T>(data, Options);
        }

        public static async ValueTask<T> FromMessagePackStream<T>(this Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return await MessagePackSerializer.DeserializeAsync<T>(stream, Options).ConfigureAwait(false);
        }

        public static async ValueTask WriteToMessagePackStream<T>(this Stream stream, T data)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            await MessagePackSerializer.SerializeAsync(stream, data, Options).ConfigureAwait(false);
        }
    }
}