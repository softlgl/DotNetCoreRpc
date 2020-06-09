using System;
using System.Collections.Generic;
using Test.IDAL;
using Test.IService;
using Test.Model;

namespace Test.Service
{
    public class PersonService:IPersonService
    {
        private readonly IPersonDal _personDal;
        public PersonService(IPersonDal personDal)
        {
            _personDal = personDal;
        }

        public bool Add(PersonModel person)
        {
            return _personDal.Add(person);
        }

        public void Delete(int id)
        {
            _personDal.Delete(id);
        }

        public PersonModel Get(int id)
        {
            return _personDal.Get(id);
        }

        public List<PersonModel> GetPersons()
        {
            return _personDal.GetPersons();
        }
    }
}
