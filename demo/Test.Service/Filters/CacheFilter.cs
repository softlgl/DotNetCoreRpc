using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetCoreRpc.Server.RpcBuilder;
using Microsoft.Extensions.Logging;

namespace Test.Service.Filters
{
    public class CacheFilter : IRpcFilter
    {
        //输出 JSON 时保持汉字原样（不转义为 \uXXXX）
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly ILogger<CacheFilter> _logger;

        public CacheFilter(ILogger<CacheFilter> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(RpcContext context, RpcRequestDelegate next)
        {
            _logger.LogInformation("CacheFilter begin, Parameters={Parameters}", JsonSerializer.Serialize(context.Parameters, JsonOptions));
            await next(context);
            _logger.LogInformation("CacheFilter end, ReturnValue={ReturnValue}", JsonSerializer.Serialize(context.ReturnValue, JsonOptions));
        }
    }
}
