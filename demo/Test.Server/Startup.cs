using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetCoreRpc.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nacos.AspNetCore.V2;
using Test.DAL;
using Test.IDAL;
using Test.IService;
using Test.Service;
using Test.Service.Configs;
using Test.Service.Filters;

namespace Test.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IPersonDal, PersonDal>()
                .AddSingleton<IPersonService,PersonService>()
                .AddSingleton<IProductDal, ProductDal>()
                .AddSingleton<IProductService, ProductService>()
                .AddSingleton(new RedisConfig { Address="127.0.0.1:6379",db=10 })
                .AddSingleton(new ElasticSearchConfig { Address = "127.0.0.1:9200" })
                .AddDotNetCoreRpcServer(options => {
                    //options.AddService<IPersonService>();
                    //options.AddService("*Service");
                    //options.AddService("IPersonService");
                    options.AddNameSpace("Test.IService");
                    options.AddFilter<CacheFilter>();
                 });
            //services.AddNacosAspNet(Configuration);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //通过中间件的方式引入
            //app.UseDotNetCoreRpc();
            app.UseRouting();
            app.UseEndpoints(endpoint => {
                endpoint.Map("/", async context=>await context.Response.WriteAsync("server start!"));
                //通过endpoint的方式引入
                endpoint.MapDotNetCoreRpc("/Test.Server6");
            });
        }
    }
}
