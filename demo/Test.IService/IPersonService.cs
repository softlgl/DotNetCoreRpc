using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Test.Model;

namespace Test.IService
{
    public interface IPersonService
    {
        PersonModel Get(int id);
        Task<List<PersonModel>> GetPersons();
        ValueTask<bool> Add(PersonModel person);
        void Delete(int id);
        Task Edit(int id);
    }
}
