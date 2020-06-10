using System;
using DotNetCoreRpc.Client;
using DotNetCoreRpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Test.IService;
using Test.Model;

namespace Test.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddDotNetCoreRpcClient()
            .AddHttpClient("TestServer", client => { client.BaseAddress = new Uri("http://localhost:34047/"); });

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            RpcClient rpcClient = serviceProvider.GetRequiredService<RpcClient>();

            IPersonService personService = rpcClient.CreateClient<IPersonService>("TestServer");
            PersonModel person = new PersonModel
            {
                Id = 1,
                Name="softlgl",
                IdCardNo = 5555555,
                BirthDay = DateTime.Now,
                HasMoney=false
            };
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
}
