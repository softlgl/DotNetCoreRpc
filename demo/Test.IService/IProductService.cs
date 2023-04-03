using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Test.Model;

namespace Test.IService
{
    public interface IProductService
    {
        ProductDto Get(int id);
        Task<List<ProductDto>> GetProducts();
        ValueTask<int> Add(ProductDto person);
        void Delete(int id);
        ValueTask Edit(int id);
    }
}
