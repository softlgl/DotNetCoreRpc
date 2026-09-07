using System;
using System.Reflection;
using System.Threading.Tasks;
using DotNetCoreRpc.Core;
using Xunit;

namespace DotNetCoreRpc.Core.Tests
{
    public class TaskUtilsTests
    {
        public class SampleService
        {
            public int Add(int a, int b) => a + b;
            public void DoNothing() { }
            public async Task<int> AddAsync(int a, int b)
            {
                await Task.Yield();
                return a + b;
            }
            public async ValueTask<string> EchoAsync(string s)
            {
                await Task.Yield();
                return s;
            }
            public async Task DoAsync()
            {
                await Task.Yield();
            }
            public async ValueTask DoValueAsync()
            {
                await Task.Yield();
            }
        }

        private static readonly MethodInfo _add = typeof(SampleService).GetMethod(nameof(SampleService.Add));
        private static readonly MethodInfo _doNothing = typeof(SampleService).GetMethod(nameof(SampleService.DoNothing));
        private static readonly MethodInfo _addAsync = typeof(SampleService).GetMethod(nameof(SampleService.AddAsync));
        private static readonly MethodInfo _echoAsync = typeof(SampleService).GetMethod(nameof(SampleService.EchoAsync));
        private static readonly MethodInfo _doAsync = typeof(SampleService).GetMethod(nameof(SampleService.DoAsync));
        private static readonly MethodInfo _doValueAsync = typeof(SampleService).GetMethod(nameof(SampleService.DoValueAsync));

        [Fact]
        public void InvokeMethod_SyncMethod_ReturnsResult()
        {
            var service = new SampleService();
            var invoker = TaskUtils.InvokeMethod(_add);
            var result = invoker.Invoke(service, new object[] { 3, 4 });
            Assert.Equal(7, result);
        }

        [Fact]
        public void InvokeMethod_VoidMethod_ReturnsNull()
        {
            var service = new SampleService();
            var invoker = TaskUtils.InvokeMethod(_doNothing);
            var result = invoker.Invoke(service, Array.Empty<object>());
            Assert.Null(result);
        }

        [Fact]
        public void InvokeMethod_CachesByMethodInfo()
        {
            var first = TaskUtils.InvokeMethod(_add);
            var second = TaskUtils.InvokeMethod(_add);
            Assert.Same(first, second);
        }

        [Fact]
        public async Task UnwrapAsync_TaskWithResult_ReturnsValue()
        {
            var value = Task.FromResult(42);
            var result = await TaskUtils.UnwrapAsync(value, value.GetType().GetTypeInfo());
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task UnwrapAsync_ValueTaskWithResult_ReturnsValue()
        {
            var value = new ValueTask<string>("hello");
            var result = await TaskUtils.UnwrapAsync(value, value.GetType().GetTypeInfo());
            Assert.Equal("hello", result);
        }

        [Fact]
        public async Task UnwrapAsync_TaskWithoutResult_ReturnsNull()
        {
            var tcs = new TaskCompletionSource<object>();
            var task = tcs.Task;
            tcs.SetResult(null);
            var result = await TaskUtils.UnwrapAsync(task, task.GetType().GetTypeInfo());
            Assert.Null(result);
        }

        [Fact]
        public async Task UnwrapAsync_ValueTaskWithoutResult_ReturnsNull()
        {
            var value = new ValueTask(Task.CompletedTask);
            var result = await TaskUtils.UnwrapAsync(value, value.GetType().GetTypeInfo());
            Assert.Null(result);
        }

        [Fact]
        public async Task UnwrapAsync_NullInput_ReturnsNull()
        {
            var result = await TaskUtils.UnwrapAsync(null, typeof(object).GetTypeInfo());
            Assert.Null(result);
        }

        [Fact]
        public async Task UnwrapAsync_AsyncMethodResult_UnwrapsCorrectly()
        {
            var service = new SampleService();
            var invoker = TaskUtils.InvokeMethod(_addAsync);
            var returnValue = invoker.Invoke(service, new object[] { 10, 20 });
            Assert.NotNull(returnValue);

            var returnValueType = returnValue.GetType().GetTypeInfo();
            Assert.True(returnValueType.IsAsync());

            var unwrapped = await TaskUtils.UnwrapAsync(returnValue, returnValueType);
            Assert.Equal(30, unwrapped);
        }
    }
}
