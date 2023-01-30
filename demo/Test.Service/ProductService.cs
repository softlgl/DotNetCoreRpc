using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Test.IDAL;
using Test.IService;
using Test.Model;
using Test.Service.Filters;

namespace Test.Service
{
    public class ProductService:IProductService
    {
        private readonly IProductDal _productDal;
        public ProductService(IProductDal productDal)
        {
            _productDal = productDal;
        }

        [LoggerFilter]
        public ValueTask<int> Add(ProductDto person)
        {
            bool result =_productDal.Add(person);
            return new ValueTask<int>(result?1:0);
        }

        public void Delete(int id)
        {
            _productDal.Delete(id);
        }

        public Task Edit(int id)
        {
            return Task.CompletedTask;
        }

        [LoggerFilter]
        public ProductDto Get(int id)
        {
            return _productDal.Get(id);
        }

        public async Task<List<ProductDto>> GetProducts()
        {
            return await Task.FromResult(_productDal.GetProducts());
        }
    }
}
