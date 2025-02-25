﻿using System;
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
using System.Collections.Generic;
using System.Net.Http;
using System.Net;

namespace Test.Client
{
    class Program
    {
        //TestServer服务名称
        const string TestServerName = "TestServer";

        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();

            IServiceCollection services = new ServiceCollection();
            services.AddLogging()
            //单机版Httpclient配置
            .AddHttpClient(TestServerName, client =>
            {
                client.BaseAddress = new Uri("http://localhost:34047");

                //http2.0或3.0
                //client.BaseAddress = new Uri("https://localhost:5001");
                //client.DefaultRequestVersion = HttpVersion.Version30;
                ////client.DefaultRequestVersion = HttpVersion.Version20;
                //client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
            })
            //.net8 or later
            //.AddAsKeyed() 
            .AddDotNetCoreRpcClient(options =>
            {
                //.net8 or later
                //options.Lifetime = ServiceLifetime.Transient;
                //options.AddAsKeyed = true;

                options.Path = "/Test.Server6";
                options.AddRpcClient<IPersonService>().AddRpcClient<IProductService>();
            });
            //基于Nacos注册中心
            //.AddNacosV2Naming(configuration)
            //.AddScoped<NacosDiscoveryDelegatingHandler>()
            //.AddHttpClient(TestServerName, client =>
            //{
            //    client.BaseAddress = new Uri($"http://{TestServerName}");
            //})
            //.AddHttpMessageHandler<NacosDiscoveryDelegatingHandler>()
            //.AddDotNetCoreRpcClient(options => {
            //    options.Path = "/Test.Server6";
            //    options.AddRpcClient<IPersonService>().AddRpcClient<IProductService>();
            //});

            using var scope = services.BuildServiceProvider().CreateScope();
            IServiceProvider serviceProvider = scope.ServiceProvider;
            //.net8 or later
            //IPersonService personService = serviceProvider.GetKeyedService<IPersonService>(TestServerName);
            IPersonService personService = serviceProvider.GetService<IPersonService>();

            //int maxCount = 10000;

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

            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
            {
                await personService.GetPersons();
            }
            stopwatch.Stop();
            Console.WriteLine($"await:{stopwatch.Elapsed.TotalMilliseconds}");

            stopwatch.Reset();
            stopwatch.Start();
            List<Task> tasks = new List<Task>();
            for (int i = 0; i < 1000; i++)
            {
                tasks.Add(personService.GetPersons());
            }
            await Task.WhenAll(tasks);
            stopwatch.Stop();
            Console.WriteLine($"tasks await:{stopwatch.Elapsed.TotalMilliseconds}");

            //.net8 or later
            //IProductService productService = serviceProvider.GetKeyedService<IProductService>(TestServerName);
            IProductService productService = serviceProvider.GetService<IProductService>();
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
            ValueTask editTask = productService.Edit(1);
            await editTask;
            Console.WriteLine($"修改Product,id=1完成");

            //BenchmarkRunner.Run<RpcClientBenchTest>();

            Console.ReadLine();
        }
    }
}
