using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using DotNetCoreRpc.Client;
using Microsoft.Extensions.DependencyInjection;
using Test.IService;
using Test.Model;

namespace Test.Client
{
    [SimpleJob(RuntimeMoniker.Net70)]
    [MemoryDiagnoser]
    public class RpcClientBenchTest
    {
        private readonly IServiceCollection services;
        private readonly IServiceProvider serviceProvider;
        private readonly IPersonService personService;

        public RpcClientBenchTest()
        {
            services = new ServiceCollection();
            services.AddHttpClient("TestServer", client => { client.BaseAddress = new Uri("http://localhost:34047/Test.Server6"); })
                .AddDotNetCoreRpcClient(options => 
                {
                    options.AddRpcClient<IPersonService>().AddRpcClient<IProductService>();
                });

            serviceProvider=services.BuildServiceProvider();
            personService = serviceProvider.GetService<IPersonService>();

            PersonModel person = new PersonModel
            {
                Id = 1,
                IdCardNo = 5555555,
                BirthDay = DateTime.Now,
                HasMoney = false
            };
            person.Name = "softlgl" + person.Id;
            personService.Add(person).GetAwaiter().GetResult();
        }

        [Benchmark]
        public void GetTest()
        {
            personService.Get(1);
        }


        [Benchmark]
        public async Task GetListTest()
        {
            await personService.GetPersons();
        }

        //[Benchmark]
        //public async void AddTest()
        //{
        //    for (int i = 0; i < maxCount; i++)
        //    {
        //        PersonModel person = new PersonModel
        //        {
        //            Id = ++i,
        //            IdCardNo = 5555555,
        //            BirthDay = DateTime.Now,
        //            HasMoney = false
        //        };
        //        person.Name = "softlgl" + person.Id;
        //        bool add = await personService.Add(person);
        //    }
        //}
    }
}
