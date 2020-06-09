using System;
using System.Collections.Generic;
using Test.Model;

namespace Test.IDAL
{
    public interface IPersonDal
    {
        PersonModel Get(int id);
        List<PersonModel> GetPersons();
        bool Add(PersonModel person);
        void Delete(int id);
    }
}
