using System;
using DotNetCoreRpc.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Test.IService;
using Test.Model;
using Xunit;

namespace DotNetCoreRpc.Tests.Integration
{
    /// <summary>
    /// 端到端集成测试：通过 WebApplicationFactory 的 TestServer 启动真实宿主，
    /// 客户端使用真实 RpcClient（代理）经完整 HTTP 管道调用服务端，
    /// 验证 MessagePack 协议 + 中间件 + 过滤器 + 服务方法的完整链路。
    /// </summary>
    public class RpcEndToEndTests
    {
        private const string ClientName = "integration";

        private static ServiceProvider BuildClientServices(WebApplicationFactory<Program> factory)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services
                .AddHttpClient(ClientName, client => client.BaseAddress = new Uri("http://localhost"))
                .ConfigurePrimaryHttpMessageHandler(() => factory.Server.CreateHandler())
                .AddDotNetCoreRpcClient(options =>
                {
                    options.Path = "/Test.Server6";
                    options.AddRpcClient<IPersonService>().AddRpcClient<IProductService>();
                });
            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task PersonService_EndToEnd_RoundTrip()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var provider = BuildClientServices(factory);
            using var scope = provider.CreateScope();
            var personService = scope.ServiceProvider.GetRequiredService<IPersonService>();

            var person = new PersonModel
            {
                Id = 7,
                IdCardNo = 123456,
                Name = "集成测试",
                BirthDay = DateTime.Now,
                HasMoney = true
            };

            Assert.True(await personService.Add(person));

            var got = personService.Get(7);
            Assert.NotNull(got);
            Assert.Equal("集成测试", got.Name);
            Assert.Equal(123456, got.IdCardNo);

            var all = await personService.GetPersons();
            Assert.Single(all);
            Assert.Equal(7, all[0].Id);

            await personService.Edit(7);
            personService.Delete(7);
            Assert.Null(personService.Get(7));
        }

        [Fact]
        public async Task ProductService_EndToEnd_RoundTrip()
        {
            using var factory = new WebApplicationFactory<Program>();
            using var provider = BuildClientServices(factory);
            using var scope = provider.CreateScope();
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

            var product = new ProductDto { Id = 1000, Name = "抗原", Price = 15.8m };
            Assert.Equal(1, await productService.Add(product));

            var got = productService.Get(1000);
            Assert.NotNull(got);
            Assert.Equal("抗原", got.Name);
            Assert.Equal(15.8m, got.Price);

            var all = await productService.GetProducts();
            Assert.Single(all);
            Assert.Equal(1000, all[0].Id);
        }
    }
}