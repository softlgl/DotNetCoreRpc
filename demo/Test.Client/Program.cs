using System;
using System.Diagnostics;
using BenchmarkDotNet.Running;
using DotNetCoreRpc.Client;
using DotNetCoreRpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Test.IService;
using Test.Model;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Nacos.V2.DependencyInjection;

namespace Test.Client
{
    class Program
    {
        //TestServer服务名称
        const string TestServerName = "TestServer";

        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();

            IServiceCollection services = new ServiceCollection();
            services.AddLogging().AddDotNetCoreRpcClient()
            //单机版Httpclient配置
            //.AddHttpClient(TestServerName, client => { client.BaseAddress = new Uri("http://localhost:34047/"); })
            //基于Nacos注册中心
            .AddNacosV2Naming(configuration)
            .AddScoped<NacosDiscoveryDelegatingHandler>()
            .AddHttpClient(TestServerName, client => {
                client.BaseAddress = new Uri($"http://{TestServerName}");
            }).AddHttpMessageHandler<NacosDiscoveryDelegatingHandler>();

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            RpcClient rpcClient = serviceProvider.GetRequiredService<RpcClient>();
            IPersonService personService = rpcClient.CreateClient<IPersonService>(TestServerName);

            int maxCount = 10000;

            //Stopwatch stopwatch = new Stopwatch();
            //Console.WriteLine($"Add开始执行");
            //stopwatch.Start();
            //for (int i = 0; i < maxCount; i++)
            //{
            //    PersonModel person = new PersonModel
            //    {
            //        Id = ++i,
            //        IdCardNo = 5555555,
            //        BirthDay = DateTime.Now,
            //        HasMoney = false
            //    };
            //    person.Name = "softlgl" + person.Id;
            //    bool add = personService.Add(person);
            //}
            //stopwatch.Stop();
            //Console.WriteLine($"Add执行完成,总用时:{stopwatch.ElapsedMilliseconds}ms");

            //stopwatch = new Stopwatch();
            //maxCount = 10000;
            //Console.WriteLine($"Get开始执行");
            //stopwatch.Start();
            //for (int i = 0; i < maxCount; i++)
            //{
            //    personService.Get(++i);
            //}
            //stopwatch.Stop();
            //Console.WriteLine($"Get执行完成,总用时:{stopwatch.ElapsedMilliseconds}ms");

            //Stopwatch stopwatch = new Stopwatch();
            //Console.WriteLine($"Add开始执行");
            //stopwatch.Start();
            //Parallel.For(0,maxCount,i=> {
            //    PersonModel person = new PersonModel
            //    {
            //        Id = ++i,
            //        IdCardNo = 5555555,
            //        BirthDay = DateTime.Now,
            //        HasMoney = false
            //    };
            //    person.Name = "softlgl" + person.Id;
            //    bool add = personService.Add(person);
            //});
            //stopwatch.Stop();
            //Console.WriteLine($"Add执行完成,总用时:{stopwatch.ElapsedMilliseconds}ms");

            //stopwatch = new Stopwatch();
            //maxCount = 10000;
            //Console.WriteLine($"Get开始执行");
            //stopwatch.Start();
            //Parallel.For(0, maxCount,i => {
            //    personService.Get(++i);
            //});
            //stopwatch.Stop();
            //Console.WriteLine($"Get执行完成,总用时:{stopwatch.ElapsedMilliseconds}ms");

            PersonModel person = new PersonModel
            {
                Id = 1,
                Name = "softlgl",
                IdCardNo = 5555555,
                BirthDay = DateTime.Now,
                HasMoney = false
            };
            bool add = personService.Add(person);
            Console.WriteLine($"添加Person:{add}");
            person = personService.Get(1);
            Console.WriteLine($"获取Person,id=1,person=[{person.ToJson()}]");
            var persons = personService.GetPersons();
            Console.WriteLine($"获取Persons,id=1,persons=[{persons.ToJson()}]");
            personService.Delete(1);
            Console.WriteLine($"删除Person,id=1完成");

            //BenchmarkRunner.Run<RpcClientBenchTest>();

            Console.ReadLine();
        }
    }
}
