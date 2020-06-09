using System;
using System.Collections.Generic;
using Test.Model;

namespace Test.IService
{
    public interface IPersonService
    {
        PersonModel Get(int id);
        List<PersonModel> GetPersons();
        bool Add(PersonModel person);
        void Delete(int id);
    }
}
