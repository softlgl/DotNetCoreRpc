using System;
namespace Test.Model
{
    public class PersonModel
    {
        public int Id { get; set; }
        public long IdCardNo { get; set; }
        public string Name { get; set; }
        public DateTime BirthDay { get; set; }
        public bool HasMoney { get; set; }
    }
}
