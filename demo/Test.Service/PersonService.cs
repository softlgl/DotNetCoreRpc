using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Test.IDAL;
using Test.IService;
using Test.Model;
using Test.Service.Filters;

namespace Test.Service
{
    public class PersonService:IPersonService
    {
        private readonly IPersonDal _personDal;
        public PersonService(IPersonDal personDal)
        {
            _personDal = personDal;
        }

        [LoggerFilter]
        public ValueTask<bool> Add(PersonModel person)
        {
            return new ValueTask<bool>(_personDal.Add(person));
        }

        public void Delete(int id)
        {
            _personDal.Delete(id);
        }

        public Task Edit(int id)
        {
            return Task.CompletedTask;
        }

        [LoggerFilter]
        public PersonModel Get(int id)
        {
            return _personDal.Get(id);
        }

        public async Task<List<PersonModel>> GetPersons()
        {
            return await Task.FromResult(_personDal.GetPersons());
        }
    }
}
