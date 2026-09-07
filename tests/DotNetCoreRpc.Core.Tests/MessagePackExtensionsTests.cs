using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DotNetCoreRpc.Core;
using Xunit;

namespace DotNetCoreRpc.Core.Tests
{
    public class MessagePackExtensionsTests
    {
        public class BusinessDto
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public double Price { get; set; }
            public List<string> Tags { get; set; } = new List<string>();
        }

        [Fact]
        public void RequestModel_Roundtrip_WithPolymorphicParameter()
        {
            var request = new RequestModel
            {
                TypeFullName = typeof(BusinessDto).FullName,
                MethodName = "GetByCondition",
                Paramters = new object[] { new BusinessDto { Id = 42, Name = "中文" } }
            };

            var bytes = request.ToMessagePackBytes();
            var back = bytes.FromMessagePack<RequestModel>();

            Assert.Equal(request.TypeFullName, back.TypeFullName);
            Assert.Equal(request.MethodName, back.MethodName);
            Assert.IsType<BusinessDto>(back.Paramters[0]);
            Assert.Equal(42, ((BusinessDto)back.Paramters[0]).Id);
        }

        [Fact]
        public void ResponseModel_Roundtrip_DataRestoredAsList()
        {
            var list = new List<BusinessDto>
            {
                new BusinessDto { Id = 1, Name = "a", Price = 1.5 },
                new BusinessDto { Id = 2, Name = "b", Price = 2.5 }
            };
            var response = new ResponseModel { Code = 200, Message = "ok", Data = list };

            var bytes = response.ToMessagePackBytes();
            var back = bytes.FromMessagePack<ResponseModel>();

            Assert.Equal(200, back.Code);
            Assert.Equal("ok", back.Message);
            Assert.IsType<List<BusinessDto>>(back.Data);
            Assert.Equal(2, ((List<BusinessDto>)back.Data).Count);
        }

        [Fact]
        public async Task RequestModel_StreamRoundtrip()
        {
            var request = new RequestModel
            {
                TypeFullName = typeof(BusinessDto).FullName,
                MethodName = "Add",
                Paramters = new object[] { new BusinessDto { Id = 5 } }
            };

            using var ms = new MemoryStream();
            await ms.WriteToMessagePackStream(request);
            ms.Position = 0;

            var back = await ms.FromMessagePackStream<RequestModel>();
            Assert.Equal(request.MethodName, back.MethodName);
            Assert.Equal(5, ((BusinessDto)back.Paramters[0]).Id);
        }

        [Fact]
        public async Task ResponseModel_StreamRoundtrip()
        {
            var response = new ResponseModel { Code = 200, Message = "success", Data = "hello" };

            using var ms = new MemoryStream();
            await ms.WriteToMessagePackStream(response);
            ms.Position = 0;

            var back = await ms.FromMessagePackStream<ResponseModel>();
            Assert.Equal("success", back.Message);
            Assert.Equal("hello", back.Data);
        }

        [Fact]
        public void ValueTypeData_UnboxedCorrectly()
        {
            // 覆盖返回值为值类型（int）时的装箱还原，客户端此前经 JsonElement，现直接 cast
            var response = new ResponseModel { Code = 200, Message = "ok", Data = 42 };

            var bytes = response.ToMessagePackBytes();
            var back = bytes.FromMessagePack<ResponseModel>();

            Assert.Equal(42, (int)back.Data);
        }
    }
}