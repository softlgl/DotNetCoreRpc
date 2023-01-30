using System;
using System.Collections.Generic;
using System.Linq;
using Test.IDAL;
using Test.Model;

namespace Test.DAL
{
    public class ProductDal : IProductDal
    {
        private List<ProductDto> products = new List<ProductDto>();

        public bool Add(ProductDto product)
        {
            if (products.Any(i => i.Id == product.Id))
            {
                return false;
            }
            products.Add(product);
            return true;
        }

        public void Delete(int id)
        {
            var person = products.FirstOrDefault(i => i.Id == id);
            if (person != null)
            {
                products.Remove(person);
            }
        }

        public ProductDto Get(int id)
        {
            return products.FirstOrDefault(i => i.Id == id);
        }

        public List<ProductDto> GetProducts()
        {
            return products;
        }
    }
}
