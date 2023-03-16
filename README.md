# DotNetCoreRpc基于.NetCore的RPC框架

#### 前言
&nbsp;&nbsp;&nbsp;&nbsp;一直以来都想实现一个简单的RPC框架。.net core不断完善之后借助其自身的便利实现一个RPC框架。框架分Server端和Client端两部分。Client端可在Console或Web端等，能运行.net core的host上运行。Server端依赖Asp.Net Core,接下来介绍大致使用，详细介绍请参阅https://www.cnblogs.com/wucy/p/13096515.html

#### 运行环境
<ul>
    <li>visual studio 2022</li>
    <li>.netstandard2.1</li>
    <li>.net5;.net6;.net7</li>
    <li>asp.net core 3.1;sp.net core 5.0;asp.net core 6.0;asp.net core 7.0</li>
</ul>

#### Client端配置使用
首先新建任意形式的.net core宿主，为了简单我使用的是Console程序,引入DotNetCoreRpc.Client包和DependencyInjection相关包
```
<PackageReference Include="DotNetCoreRpc.Client" Version="1.1.2" />
```
引入自己的服务接口包我这里是Test.IService,只需要引入interface层即可,写入如下测试代码,具体代码可参阅demo，由于DotNetCoreRpc通信是基于HttpClientFactory的，所以需要注册HttpClientFactory
```cs
class Program
{
    static void Main(string[] args)
    {
        IServiceCollection services = new ServiceCollection();
        //*注册DotNetCoreRpcClient
        services.AddDotNetCoreRpcClient()
        //*通信基于HttpClientFactory,自行注册即可
        .AddHttpClient("TestServer", client => { client.BaseAddress = new Uri("http://localhost:34047/"); });
        //.AddHttpClient(TestServerName, client => { client.BaseAddress = new Uri("http://localhost:34047/Test.Server6"); });

        IServiceProvider serviceProvider = services.BuildServiceProvider();
        //*RpcClient使用这个类创建具体服务代理
        RpcClient rpcClient = serviceProvider.GetRequiredService<RpcClient>();

        //IPersonService是我引入的服务包interface，需要提供ServiceName,即AddHttpClient的名称
        IPersonService personService = rpcClient.CreateClient<IPersonService>("TestServer");
        PersonModel person = new PersonModel
        {
            Id = 1,
            Name = "softlgl",
            IdCardNo = 5555555,
            BirthDay = DateTime.Now,
            HasMoney = false
        };
        bool add = await personService.Add(person);
        Console.WriteLine($"添加Person1:{add}");
        person = personService.Get(1);
        Console.WriteLine($"获取Person,id=1,person=[{person.ToJson()}]");
        person = new PersonModel
        {
            Id = 2,
            Name = "yi念之间",
            IdCardNo = 666888,
            BirthDay = DateTime.Now,
            HasMoney = false
        };
        add = await personService.Add(person);
        Console.WriteLine($"添加Person2:{add}");
        var persons = await personService.GetPersons();
        Console.WriteLine($"获取Persons,persons=[{persons.ToJson()}]");
        await personService.Edit(1);
        Console.WriteLine($"修改Person,id=1完成");
        personService.Delete(1);
        Console.WriteLine($"删除Person,id=1完成");
        persons = await personService.GetPersons();
        Console.WriteLine($"最后获取Persons,persons=[{persons.ToJson()}]");

        IProductService productService = rpcClient.CreateClient<IProductService>(TestServerName);
        ProductDto product = new ProductDto
        {
            Id = 1000,
            Name="抗原",
            Price = 158.22M
        };
        int productAddResult = await productService.Add(product);
        Console.WriteLine($"添加Product1:{productAddResult==1}");
        product = productService.Get(1000);
        Console.WriteLine($"获取添加Product1,id=1000,person=[{product.ToJson()}]");
        product = new ProductDto
        {
            Id = 2000,
            Name = "N95口罩",
            Price = 35.5M
        };
        productAddResult = await productService.Add(product);
        Console.WriteLine($"添加Product2:{productAddResult == 1}");
        product = productService.Get(2000);
        Console.WriteLine($"获取添加Product2,id=2000,person=[{product.ToJson()}]");
        var products = await productService.GetProducts();
        Console.WriteLine($"products=[{products.ToJson()}]");
    }
}
```
#### Server端配置使用

新建一个最简单的Asp.net Core项目,我这里的Demo是新建的Asp.net Core的空项目,引入DotNetCoreRpc.Server包
```
<PackageReference Include="DotNetCoreRpc.Server" Version="1.1.2" />
```
然后添加注入和相关中间件
```cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IPersonDal, PersonDal>()
            .AddSingleton<IPersonService,PersonService>()
            .AddSingleton(new RedisConfig { Address="127.0.0.1:6379",db=10 })
            .AddSingleton(new ElasticSearchConfig { Address = "127.0.0.1:9200" })
            //*注册DotNetCoreRpcServer
            .AddDotNetCoreRpcServer(options => {
                //*确保以下添加的服务已经被注册到DI容器
                
                //添加作为服务的接口
                //options.AddService<IPersonService>();
                
                //或添加作为服务的接口以xxx为结尾的接口
                //options.AddService("*Service");
                
                //或添加具体名称为xxx的接口
                //options.AddService("IPersonService");
                //或具体命名空间下的接口
                options.AddNameSpace("Test.IService");
                
                //添加全局过滤器
                options.AddFilter<CacheFilter>();
             });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        //通过中间件的方式引入
        //app.UseDotNetCoreRpc();
        app.UseRouting();
        app.UseEndpoints(endpoint => {
            endpoint.Map("/", async context=>await context.Response.WriteAsync("server start!"));
            //通过endpoint的方式引入
            endpoint.MapDotNetCoreRpc();
            //endpoint.MapDotNetCoreRpc("/Test.Server6");
        });
    }
}
```
如果是ASP.NET Core 6的Minimal Api则使用以下方式
```cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPersonDal, PersonDal>()
               .AddSingleton<IPersonService, PersonService>()
               .AddSingleton<IProductDal, ProductDal>()
               .AddSingleton<IProductService, ProductService>()
               .AddSingleton(new RedisConfig { Address = "127.0.0.1:6379", db = 10 })
               .AddSingleton(new ElasticSearchConfig { Address = "127.0.0.1:9200" })
               .AddDotNetCoreRpcServer(options => {
                    //options.AddService<IPersonService>();
                    //options.AddService("*Service");
                    //options.AddService("IPersonService");
                    options.AddNameSpace("Test.IService");
                    options.AddFilter<CacheFilter>();
               });

app.UseDotNetCoreRpc();
//app.UseDotNetCoreRpc("/Test.Server6");
app.MapGet("/", () => "Hello World!");

app.Run();
```
过滤器的使用方式,可添加到类上或者方法上或者全局注册,优先级 方法>类>全局注册，RpcFilterAttribute是基于管道模式执行的，可支持注册多个Filter，支持属性注入。
```cs
public class CacheFilter : RpcFilterAttribute
{
    private readonly ElasticSearchConfig _elasticSearchConfig;

    [FromServices]
    private RedisConfig RedisConfig { get; set; }

    [FromServices]
    private ILogger<CacheFilter> Logger { get; set; }

    public CacheFilter(ElasticSearchConfig elasticSearchConfig)
    {
        _elasticSearchConfig = elasticSearchConfig;
    }
    public override async Task InvokeAsync(RpcContext context, RpcRequestDelegate next)
    {
        Logger.LogInformation($"CacheFilter begin,Parameters={context.Parameters}");
        await next(context);
        Logger.LogInformation($"CacheFilter end,ReturnValue={context.ReturnValue.ToJson()}");
    }
}
```
