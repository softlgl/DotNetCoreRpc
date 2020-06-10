# DotNetCoreRpc基于.NetCore的RPC框架

#### 前言
&nbsp;&nbsp;&nbsp;&nbsp;一直以来都想实现一个简单的RPC框架。.net core不断完善之后借助其自身的便利实现一个RPC框架。框架分Server端和Client端两部分。Client端可在Console或Web端等，能运行.net core的host上运行。Server端依赖Asp.Net Core。

#### Client端配置使用
首先新建任意形式的.net core宿主，为了简单我使用的是Console程序,引入DotNetCoreRpc.Client包和DependencyInjection相关包
```
<PackageReference Include="DotNetCoreRpc.Client" Version="1.0.0" />
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
            services.AddDotNetCoreRpcClient()
            .AddHttpClient("TestServer", client => { client.BaseAddress = new Uri("http://localhost:34047/"); });

            IServiceProvider serviceProvider = services.BuildServiceProvider();
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
