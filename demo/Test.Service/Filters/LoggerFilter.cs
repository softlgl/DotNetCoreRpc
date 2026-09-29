using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using DotNetCoreRpc.Server.RpcBuilder;
using Microsoft.Extensions.Logging;

namespace Test.Service.Filters
{
    public class LoggerFilter : IRpcFilter
    {
        //输出 JSON 时保持汉字原样（不转义为 \uXXXX）
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly ILogger<LoggerFilter> _logger;

        public LoggerFilter(ILogger<LoggerFilter> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(RpcContext context, RpcRequestDelegate next)
        {
            _logger.LogInformation("LoggerFilter begin, Parameters={Parameters}", JsonSerializer.Serialize(context.Parameters[0], JsonOptions));
            await next(context);
            _logger.LogInformation("LoggerFilter end, ReturnValue={ReturnValue}", JsonSerializer.Serialize(context.ReturnValue, JsonOptions));
        }
    }
}
