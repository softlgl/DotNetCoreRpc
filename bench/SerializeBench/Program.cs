using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetCoreRpc.Core;
using MessagePack;
using MessagePack.Resolvers;

// ============================================================================
// 序列化协议可行性验证（隔离原型，不改动任何 src/demo 代码）
//
// 对比三种方案在真实 RPC 载荷（RequestModel / ResponseModel）上的表现：
//   1) JSON-反射  （System.Text.Json 默认，即当前 DotNetCoreRpc 现状）
//   2) JSON-源生成（System.Text.Json SourceGenerator，方案C）
//   3) MessagePack（方案A，二进制）
//
// 只读复用 DotNetCoreRpc.Core 里的 RequestModel/ResponseModel，
// 证明"不改使用方式"即可在内部切换序列化实现。
// ============================================================================

// 方案C用的源生成上下文（JSON 反射模式不需要它）
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(RequestModel))]
[JsonSerializable(typeof(ResponseModel))]
internal partial class RpcJsonContext : JsonSerializerContext { }

// 模拟一个简单的业务 DTO（对应 demo 里的 Person/Product 结构层级）
public class BusinessPayload
{
    public int Id { get; set; }
    public string Name { get; set; }
    public double Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
}

internal static class Program
{
    // 注意：不能用数组——RT 反射无法恢复业务类型，用 List 尽量贴近真实 RPC 载荷。
    private static void Main()
    {
        Console.WriteLine("DotNetCoreRpc 序列化协议可行性验证\n");
        Console.WriteLine($"{nameof(RequestModel)} 序列化的时候 Parameters 是 object[]，业务数据类型在运行时才能确定——这是 JSON/Binary 是否可行的分水岭。");

        // ---- 构造一次典型 RPC 请求载荷 ----
        var requestArgument = new BusinessPayload
        {
            Id = 42,
            Name = "商品-中文名称",
            Price = 19.99,
            CreatedAt = DateTime.Now,
            Tags = new List<string> { "tag-a", "tag-b", "tag-c" }
        };
        var requestModel = new RequestModel
        {
            TypeFullName = typeof(BusinessPayload).FullName,
            MethodName = "GetByCondition",
            Paramters = new object[] { requestArgument }
        };

        // ---- 构造一次典型 RPC 响应载荷（业务对象列表，模拟分页）----
        var list = Enumerable.Range(0, 500).Select(i => new BusinessPayload
        {
            Id = i,
            Name = $"item-{i}",
            Price = i * 1.5,
            CreatedAt = DateTime.Now.AddDays(-i),
            Tags = new List<string> { "a", "b" }
        }).ToList();
        var responseModel = new ResponseModel
        {
            Code = 200,
            Message = "success",
            Data = list
        };

        // 预热（外层 try/catch 定位究竟是哪个模型会抛异常）
        foreach (var (label, action) in new (string, Action)[]
        {
            ("JSON-反射（现状）", () => JsonReflection.Roundtrip(requestModel, responseModel)),
            ("MessagePack（方案A）", () => MessagePackBinary.Roundtrip(requestModel, responseModel)),
        })
        {
            try { for (int i = 0; i < 3; i++) action(); }
            catch (Exception ex) { Console.WriteLine($"!! [{label}] 预热失败: {ex}"); }
        }
        MessagePackOverJson.AnyPrototype(requestModel, responseModel);

        const int iterations = 2000;
        Run("JSON-反射（现状）", () => JsonReflection.Roundtrip(requestModel, responseModel), iterations);
        Run("MessagePack（方案A）", () => MessagePackBinary.Roundtrip(requestModel, responseModel), iterations);
    }

    private static void Run(string label, Action body, int iterations)
    {
        // 手动预热 + 计时，避免引 BenchmarkDotNet 的依赖下载
        for (int i = 0; i < 100; i++) body();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++) body();
        sw.Stop();

        Console.WriteLine($"[{label}]  共 {iterations} 轮      耗时 {sw.ElapsedMilliseconds} ms");
    }

    // ==========================================================================
    // 方案0：当前 JSON-反射（也就是 JsonExtensions 现状，直接复用静态方法）
    // ==========================================================================
    internal static class JsonReflection
    {
        public static void Roundtrip(RequestModel req, ResponseModel resp)
        {
            var bytes = req.ToUtf8Bytes();
            var reqBack = bytes.FromJson<RequestModel>();
            var respBytes = resp.ToUtf8Bytes();
            var respBack = respBytes.FromJson<ResponseModel>();
        }
    }

    // ==========================================================================
    // 方案C：JSON 源生成（去掉反射分配，但 Data/Paramters 仍是 object）
    // ==========================================================================
    internal static class JsonSourceGen
    {
        public static void Roundtrip(RequestModel req, ResponseModel resp)
        {
            var reqBytes = JsonSerializer.SerializeToUtf8Bytes(req, RpcJsonContext.Default.RequestModel);
            var reqBack = JsonSerializer.Deserialize(reqBytes, RpcJsonContext.Default.RequestModel);
            var respBytes = JsonSerializer.SerializeToUtf8Bytes(resp, RpcJsonContext.Default.ResponseModel);
            var respBack = JsonSerializer.Deserialize(respBytes, RpcJsonContext.Default.ResponseModel);
        }
    }

    // ==========================================================================
    // 方案A：MessagePack 二进制（typeless 以恢复 object 多态）
    // ==========================================================================
    internal static class MessagePackBinary
    {
        internal static readonly MessagePackSerializerOptions _options =
            MessagePackSerializerOptions.Standard
                .WithResolver(TypelessContractlessStandardResolver.Instance);

        public static void Roundtrip(RequestModel req, ResponseModel resp)
        {
            var reqBytes = MessagePackSerializer.Serialize(req, _options);
            var reqBack = MessagePackSerializer.Deserialize<RequestModel>(reqBytes, _options);
            var respBytes = MessagePackSerializer.Serialize(resp, _options);
            var respBack = MessagePackSerializer.Deserialize<ResponseModel>(respBytes, _options);
        }
    }

    // ==========================================================================
    // 一个能说明问题的附加测量：只序列化响应（含 500 条的业务对象列表）的体积
    // ==========================================================================
    internal static class MessagePackOverJson
    {
        public static void AnyPrototype(RequestModel req, ResponseModel resp)
        {
            var json = resp.ToUtf8Bytes();
            var mp = MessagePackSerializer.Serialize(resp, MessagePackBinary._options);

            Console.WriteLine($"\n响应载荷体积对比（500 条业务对象，原始字节）：");
            Console.WriteLine($"  JSON-反射   : {json.Length,6} bytes");
            Console.WriteLine($"  MessagePack : {mp.Length,6} bytes   (省 {100 - mp.Length * 100 / json.Length}%)");
            Console.WriteLine();
        }
    }
}