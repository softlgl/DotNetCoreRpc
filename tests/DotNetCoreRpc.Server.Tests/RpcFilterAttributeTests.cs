using System;
using System.Linq;
using DotNetCoreRpc.Server.RpcBuilder;
using Xunit;

namespace DotNetCoreRpc.Server.Tests
{
    public class RpcFilterAttributeTests
    {
        [Fact]
        public void 构造函数_Null类型_抛出ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RpcFilterAttribute(null));
        }

        [Fact]
        public void 构造函数_保存FilterType与Order()
        {
            var attribute = new RpcFilterAttribute(typeof(FilterA)) { Order = 5 };

            Assert.Equal(typeof(FilterA), attribute.FilterType);
            Assert.Equal(5, attribute.Order);
        }

        [Fact]
        public void 特性只允许挂在类与方法上()
        {
            var usage = typeof(RpcFilterAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
                .Cast<AttributeUsageAttribute>().Single();

            Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Class));
            Assert.True(usage.ValidOn.HasFlag(AttributeTargets.Method));
            Assert.False(usage.ValidOn.HasFlag(AttributeTargets.Property));
        }

        [Fact]
        public void 同一成员允许挂多个过滤器()
        {
            var usage = typeof(RpcFilterAttribute).GetCustomAttributes(typeof(AttributeUsageAttribute), false)
                .Cast<AttributeUsageAttribute>().Single();

            Assert.True(usage.AllowMultiple);
            Assert.True(usage.Inherited);
        }
    }
}
