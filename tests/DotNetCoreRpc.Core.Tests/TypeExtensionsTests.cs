using System.Reflection;
using System.Threading.Tasks;
using DotNetCoreRpc.Core;
using Xunit;

namespace DotNetCoreRpc.Core.Tests
{
    public class TypeExtensionsTests
    {
        [Fact]
        public void IsTask_TrueForTask()
        {
            Assert.True(typeof(Task).GetTypeInfo().IsTask());
        }

        [Fact]
        public void IsTask_FalseForTaskOfT()
        {
            Assert.False(typeof(Task<int>).GetTypeInfo().IsTask());
        }

        [Fact]
        public void IsTaskWithResult_TrueForTaskOfT()
        {
            Assert.True(typeof(Task<int>).GetTypeInfo().IsTaskWithResult());
            Assert.True(typeof(Task<string>).GetTypeInfo().IsTaskWithResult());
        }

        [Fact]
        public void IsTaskWithResult_FalseForTask()
        {
            Assert.False(typeof(Task).GetTypeInfo().IsTaskWithResult());
        }

        [Fact]
        public void IsValueTask_TrueForValueTask()
        {
            Assert.True(typeof(ValueTask).GetTypeInfo().IsValueTask());
        }

        [Fact]
        public void IsValueTask_FalseForValueTaskOfT()
        {
            Assert.False(typeof(ValueTask<int>).GetTypeInfo().IsValueTask());
        }

        [Fact]
        public void IsValueTaskWithResult_TrueForValueTaskOfT()
        {
            Assert.True(typeof(ValueTask<int>).GetTypeInfo().IsValueTaskWithResult());
            Assert.True(typeof(ValueTask<string>).GetTypeInfo().IsValueTaskWithResult());
        }

        [Fact]
        public void IsValueTaskWithResult_FalseForValueTask()
        {
            Assert.False(typeof(ValueTask).GetTypeInfo().IsValueTaskWithResult());
        }

        [Fact]
        public void IsAsync_TrueForAllTaskVariants()
        {
            Assert.True(typeof(Task).GetTypeInfo().IsAsync());
            Assert.True(typeof(Task<int>).GetTypeInfo().IsAsync());
            Assert.True(typeof(ValueTask).GetTypeInfo().IsAsync());
            Assert.True(typeof(ValueTask<int>).GetTypeInfo().IsAsync());
        }

        [Fact]
        public void IsAsync_FalseForNonTaskTypes()
        {
            Assert.False(typeof(int).GetTypeInfo().IsAsync());
            Assert.False(typeof(string).GetTypeInfo().IsAsync());
            Assert.False(typeof(object).GetTypeInfo().IsAsync());
        }

        [Fact]
        public void IsTask_ThrowsOnNull()
        {
            Assert.Throws<System.ArgumentNullException>(() => ((System.Reflection.TypeInfo)null).IsTask());
        }
    }
}
