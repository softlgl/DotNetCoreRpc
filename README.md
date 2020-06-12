# DotNetCoreRpc基于.NetCore的RPC框架

#### 前言
&nbsp;&nbsp;&nbsp;&nbsp;一直以来都想实现一个简单的RPC框架。.net core不断完善之后借助其自身的便利实现一个RPC框架。框架分Server端和Client端两部分。Client端可在Console或Web端等，能运行.net core的host上运行。Server端依赖Asp.Net Core,接下来介绍大致使用，代码不完成,具体使用方式可参阅Demo https://github.com/softlgl/DotNetCoreRpc/edit/master/demo

#### 运行环境
<ul>
    <li>visual studio 2019</li>
    <li>.net standard 2.1</li>
    <li>asp.net core 3.1</li>
</ul>

#### Client端配置使用
首先新建任意形式的.net core宿主，为了简单我使用的是Console程序,引入DotNetCoreRpc.Client包和DependencyInjection相关包
```
<PackageReference Include="DotNetCoreRpc.Client" Version="1.0.2" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="3.1.4" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="3.1.4" />
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

        IServiceProvider serviceProvider = services.BuildServiceProvider();
        //*RpcClient使用这个类创建具体服务代理
        RpcClient rpcClient = serviceProvider.GetRequiredService<RpcClient>();

        //IPersonService是我引入的服务包interface，需要提供ServiceName,即AddHttpClient的名称
        IPersonService personService = rpcClient.CreateClient<IPersonService>("TestServer");
        PersonModel person = new PersonModel
        {
            Id = 1,
            Name="softlgl",
            IdCardNo = 5555555,
            BirthDay = DateTime.Now,
            HasMoney=false
        };
        //可以和调用本地代码一样爽了
        bool add = personService.Add(person);
        Console.WriteLine($"添加Person:{add}");
        person = personService.Get(1);
        Console.WriteLine($"获取Person,id=1,person=[{person.ToJson()}]");
        var persons = personService.GetPersons();
        Console.WriteLine($"获取Persons,id=1,persons=[{persons.ToJson()}]");
        personService.Delete(1);
        Console.WriteLine($"删除Person,id=1完成");

        Console.ReadLine();
    }
}
```
#### Server端配置使用

新建一个最简单的Asp.net Core项目,我这里的Demo是新建的Asp.net Core的空项目,引入DotNetCoreRpc.Server包
```
<PackageReference Include="DotNetCoreRpc.Server" Version="1.0.2" />
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
        //添加中间件
        app.UseDotNetCoreRpc();
    }
}
```
过滤器的使用方式,可添加到类上或者方法上或者全局注册,优先级 方法>类>全局注册，RpcFilterAttribute是基于管道模式执行的，可支持注册多个Filter，支持属性注入。
```cs
public class CacheFilter : RpcFilterAttribute
{
    private readonly ElasticSearchConfig _elasticSearchConfig;

    //支持属性注入public private都可以
    [FromServices]
    private RedisConfig RedisConfig { get; set; }

    public CacheFilter(ElasticSearchConfig elasticSearchConfig)
    {
        _elasticSearchConfig = elasticSearchConfig;
    }
    public override async Task InvokeAsync(RpcContext context, RpcRequestDelegate next)
    {
        Debug.WriteLine($"CacheFilter begin,Parameters={context.Parameters}");
        await next(context);
        Debug.WriteLine($"CacheFilter end,ReturnValue={context.ReturnValue.ToJson()}");
    }
 }
```
