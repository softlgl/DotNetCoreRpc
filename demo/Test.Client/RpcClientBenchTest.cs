using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using DotNetCoreRpc.Client;
using Microsoft.Extensions.DependencyInjection;
using Test.IService;
using Test.Model;

namespace Test.Client
{
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [MemoryDiagnoser]
    [RPlotExporter]
    public class RpcClientBenchTest
    {
        private readonly IServiceCollection services;
        private readonly IServiceProvider serviceProvider;
        private readonly RpcClient rpcClient;
        private readonly IPersonService personService;

        [Params(1000)]
        public int maxCount;

        public RpcClientBenchTest()
        {
            services = new ServiceCollection();
            services.AddDotNetCoreRpcClient()
            .AddHttpClient("TestServer", client => { client.BaseAddress = new Uri("http://localhost:34047/"); });

            serviceProvider=services.BuildServiceProvider();
            rpcClient = serviceProvider.GetRequiredService<RpcClient>();
            rpcClient.CreateClient<IPersonService>("TestServer");
        }

        [Benchmark]
        public void GetTest()
        {
            for (int i = 0; i < maxCount; i++)
            {
                personService.Get(++i);
            }
        }

        [Benchmark]
        public async void AddTest()
        {
            for (int i = 0; i < maxCount; i++)
            {
                PersonModel person = new PersonModel
                {
                    Id = ++i,
                    IdCardNo = 5555555,
                    BirthDay = DateTime.Now,
                    HasMoney = false
                };
                person.Name = "softlgl" + person.Id;
                bool add = await personService.Add(person);
            }
        }
    }
}
