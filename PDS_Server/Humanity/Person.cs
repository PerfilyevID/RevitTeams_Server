using System;

namespace PDS_Server.Humanity
{
    public class Person
    {
        public static Person[] Persons { get; } = new Person[]
        {
            new Person("Игорь", "Перфильев", "perfilev-id@revit-teams.ru"),
            new Person("Игорь", "Перфильев", "support@revit-teams.ru"),
            new Person("Илья", "Бородатов", "support@revit-teams.ru"),
            new Person("Олег", "Бараксанов",  "support@revit-teams.ru"),
            new Person("Артем", "Шерстнов",  "support@revit-teams.ru")
        };
        public static Person GetRandomPerson() 
        { 
            return Persons[new Random().Next(1, Persons.Length - 1)]; 
        }
        public Person(string firstName, string lastName, string email)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
        }
        public string FirstName { get; }
        public string LastName { get; }
        public string Email { get; }
    }
}
