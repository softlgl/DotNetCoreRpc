using Microsoft.AspNetCore.Http;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using DotNetCoreRpc.Core;

namespace DotNetCoreRpc.Server
{
    internal static class HttpRequestExtensions
    {
        public static async Task<T> ReadAsync<T>(this HttpRequest request) where T : class, new()
        {
            var result = (await ReadStringAsync(request))?.FromJson<T>();
            return result;
        }

        public static async Task<string> ReadStringAsync(this HttpRequest request)
        {
            var reader = request.BodyReader;
            StringBuilder builder= new StringBuilder();

            while (true)
            {
                ReadResult readResult = await reader.ReadAsync();
                var buffer = readResult.Buffer;
                SequencePosition? position = null;

                do
                {
                    position = buffer.PositionOf((byte)'\n');
                    if (position != null)
                    {
                         var readOnlySequence = buffer.Slice(0, position.Value);
                         AddString(builder, in readOnlySequence);

                         buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                    }
                }
                while (position != null);

                if (readResult.IsCompleted && buffer.Length > 0)
                {
                    AddString(builder, in buffer);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (readResult.IsCompleted)
                {
                    break;
                }
            }

            return builder.ToString();
        }

        private static void AddString(StringBuilder results, in ReadOnlySequence<byte> readOnlySequence)
        {
            ReadOnlySpan<byte> span = readOnlySequence.IsSingleSegment ? readOnlySequence.First.Span : readOnlySequence.ToArray().AsSpan();
            results.Append(Encoding.UTF8.GetString(span));
        }
    }
}
