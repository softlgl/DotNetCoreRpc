using System;
using System.Collections.Generic;
using System.Linq;
using Test.IDAL;
using Test.Model;

namespace Test.DAL
{
    public class PersonDal:IPersonDal
    {
        private List<PersonModel> persons = new List<PersonModel>();

        public bool Add(PersonModel person)
        {
            if (persons.Any(i => i.Id == person.Id))
            {
                return false;
            }
            persons.Add(person);
            return true;
        }

        public void Delete(int id)
        {
            var person = persons.FirstOrDefault(i => i.Id == id);
            if (person != null)
            {
                persons.Remove(person);
            }
        }

        public PersonModel Get(int id)
        {
            return persons.FirstOrDefault(i => i.Id == id);
        }

        public List<PersonModel> GetPersons()
        {
            return persons;
        }
    }
}
