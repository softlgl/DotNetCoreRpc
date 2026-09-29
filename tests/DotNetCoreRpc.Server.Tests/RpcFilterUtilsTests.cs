using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using DotNetCoreRpc.Server.RpcBuilder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetCoreRpc.Server.Tests
{
    // ---------- 测试辅助：过滤器与标记类 ----------

    public class FilterA : IRpcFilter
    {
        public Task InvokeAsync(RpcContext context, RpcRequestDelegate next) => next(context);
    }

    public class FilterB : IRpcFilter
    {
        public Task InvokeAsync(RpcContext context, RpcRequestDelegate next) => next(context);
    }

    public class TestDep
    {
        public string Name => "injected";
    }

    public class NeedDepFilter : IRpcFilter
    {
        public string DepName { get; }

        public NeedDepFilter(TestDep dep)
        {
            DepName = dep.Name;
        }

        public Task InvokeAsync(RpcContext context, RpcRequestDelegate next) => next(context);
    }

    [RpcFilter(typeof(FilterA))]
    public class ClassLevelMarkedService
    {
        public void NoMark() { }

        [RpcFilter(typeof(FilterB))]
        public void MethodMarked() { }
    }

    public class OrderMarkedService
    {
        [RpcFilter(typeof(FilterA), Order = 2)]
        [RpcFilter(typeof(FilterB), Order = 1)]
        public void Method() { }
    }

    [RpcFilter(typeof(FilterA))]
    public class ClassAndMethodSameOrderService
    {
        [RpcFilter(typeof(FilterB))]
        public void Method() { }
    }

    public class MethodLevelOnlyService
    {
        [RpcFilter(typeof(FilterB))]
        public void Method() { }
    }

    public class UnmarkedService
    {
        public void Method() { }
    }

    // ---------- 测试 ----------

    public class RpcFilterUtilsTests
    {
        private static RpcContext ContextFor<T>(string methodName)
        {
            return new RpcContext { Method = typeof(T).GetMethod(methodName) };
        }

        [Fact]
        public void GetFilterAttributes_方法级标记_只返回方法级过滤器()
        {
            var attributes = RpcFilterUtils.GetFilterAttributes(ContextFor<MethodLevelOnlyService>(nameof(MethodLevelOnlyService.Method)));

            Assert.Single(attributes);
            Assert.Equal(typeof(FilterB), attributes[0].FilterType);
        }

        [Fact]
        public void GetFilterAttributes_类级标记_未挂方法标记时返回类级过滤器()
        {
            var attributes = RpcFilterUtils.GetFilterAttributes(ContextFor<ClassLevelMarkedService>(nameof(ClassLevelMarkedService.NoMark)));

            Assert.Single(attributes);
            Assert.Equal(typeof(FilterA), attributes[0].FilterType);
        }

        [Fact]
        public void GetFilterAttributes_类级与方法级合并_类级在前()
        {
            var attributes = RpcFilterUtils.GetFilterAttributes(ContextFor<ClassLevelMarkedService>(nameof(ClassLevelMarkedService.MethodMarked)));

            Assert.Equal(2, attributes.Length);
            Assert.Equal(typeof(FilterA), attributes[0].FilterType);
            Assert.Equal(typeof(FilterB), attributes[1].FilterType);
        }

        [Fact]
        public void GetFilterAttributes_按Order升序排序()
        {
            var attributes = RpcFilterUtils.GetFilterAttributes(ContextFor<OrderMarkedService>(nameof(OrderMarkedService.Method)));

            Assert.Equal(2, attributes.Length);
            Assert.Equal(typeof(FilterB), attributes[0].FilterType); // Order=1
            Assert.Equal(typeof(FilterA), attributes[1].FilterType); // Order=2
        }

        [Fact]
        public void GetFilterAttributes_相同Order_类级在前方法级在后_稳定排序()
        {
            var attributes = RpcFilterUtils.GetFilterAttributes(ContextFor<ClassAndMethodSameOrderService>(nameof(ClassAndMethodSameOrderService.Method)));

            Assert.Equal(2, attributes.Length);
            Assert.Equal(typeof(FilterA), attributes[0].FilterType); // 类级
            Assert.Equal(typeof(FilterB), attributes[1].FilterType); // 方法级
        }

        [Fact]
        public void GetFilterAttributes_无任何标记_返回空数组()
        {
            var attributes = RpcFilterUtils.GetFilterAttributes(ContextFor<UnmarkedService>(nameof(UnmarkedService.Method)));

            Assert.Empty(attributes);
        }

        [Fact]
        public void CreateFilterInstance_已注册到DI_返回注册实例_尊重生命周期()
        {
            var services = new ServiceCollection();
            services.AddSingleton<FilterA>();
            using var provider = services.BuildServiceProvider();

            var first = RpcFilterUtils.CreateFilterInstance(provider, typeof(FilterA));
            var second = RpcFilterUtils.CreateFilterInstance(provider, typeof(FilterA));

            Assert.IsAssignableFrom<IRpcFilter>(first);
            Assert.Same(first, second); // singleton 注册：同一实例
        }

        [Fact]
        public void CreateFilterInstance_未注册DI_用ActivatorUtilities构造函数注入()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new TestDep());
            using var provider = services.BuildServiceProvider();

            var filter = RpcFilterUtils.CreateFilterInstance(provider, typeof(NeedDepFilter)) as NeedDepFilter;

            Assert.NotNull(filter);
            Assert.Equal("injected", filter.DepName);
        }

        [Fact]
        public void CreateFilterInstance_未注册DI_每次调用返回新实例()
        {
            var services = new ServiceCollection();
            services.AddSingleton(new TestDep());
            using var provider = services.BuildServiceProvider();

            var first = RpcFilterUtils.CreateFilterInstance(provider, typeof(NeedDepFilter));
            var second = RpcFilterUtils.CreateFilterInstance(provider, typeof(NeedDepFilter));

            Assert.NotSame(first, second);
        }

        [Fact]
        public async Task Pipeline_按注册顺序执行_先入先出()
        {
            var builder = new AspectPipelineBuilder();
            var log = new List<string>();
            builder.Use(async (ctx, next) =>
            {
                log.Add("outer-before");
                await next(ctx);
                log.Add("outer-after");
            });
            builder.Use(async (ctx, next) =>
            {
                log.Add("inner-before");
                await next(ctx);
                log.Add("inner-after");
            });
            var pipeline = builder.Build(ctx =>
            {
                log.Add("endpoint");
                return Task.CompletedTask;
            });

            await pipeline(new RpcContext());

            Assert.Equal(new[] { "outer-before", "inner-before", "endpoint", "inner-after", "outer-after" }, log);
        }
    }
}
