using DotNetCoreRpc.Server;
using Test.DAL;
using Test.IDAL;
using Test.IService;
using Test.Service;
using Test.Service.Configs;
using Test.Service.Filters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IPersonDal, PersonDal>()
               .AddSingleton<IPersonService, PersonService>()
               .AddSingleton<IProductDal, ProductDal>()
               .AddSingleton<IProductService, ProductService>()
               .AddSingleton(new RedisConfig { Address = "127.0.0.1:6379", db = 10 })
               .AddSingleton(new ElasticSearchConfig { Address = "127.0.0.1:9200" })
               .AddDotNetCoreRpcServer(options => {
                    options.AddFilter<CacheFilter>();
               });

var app = builder.Build();

app.UseDotNetCoreRpc("/Test.Server6");
app.MapGet("/", () => "Hello World!");

app.Run();
