using DotNetCoreRpc.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nacos.AspNetCore.V2;
using Nacos.V2.Naming.Dtos;
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
               .AddKeyedSingleton("elasticSearchConfig", new ElasticSearchConfig { Address = "127.0.0.1:9200" })
               .AddDotNetCoreRpcServer(options => {
                    options.AddFilter<CacheFilter>();
               });

//builder.Services.AddNacosAspNet(builder.Configuration);

//添加ttp2和Http3支持
//builder.WebHost.ConfigureKestrel((context, options) =>
//{
//    options.ListenAnyIP(5001, listenOptions =>
//    {
//        listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
//        listenOptions.UseHttps();
//    });
//});

var app = builder.Build();

app.UseDotNetCoreRpc("/Test.Server6");
app.MapGet("/", () => "Hello World!");

app.Run();
