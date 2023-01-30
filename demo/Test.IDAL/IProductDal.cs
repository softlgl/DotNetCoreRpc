using System.Collections.Generic;
using Test.Model;

namespace Test.IDAL
{
    public interface IProductDal
    {
        ProductDto Get(int id);
        List<ProductDto> GetProducts();
        bool Add(ProductDto product);
        void Delete(int id);
    }
}