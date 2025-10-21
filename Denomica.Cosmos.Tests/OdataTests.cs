using Denomica.Cosmos.Model;
using Denomica.Cosmos.Odata;
using Denomica.OData;
using Microsoft.Extensions.DependencyInjection;
using Schema.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.Cosmos.Tests
{
    [TestClass]
    public class OdataTests
    {

        [ClassInitialize]
        public static async Task ClassInit(TestContext context)
        {
            var provider = context.CreateServiceProvider();
            Adapter = provider.GetRequiredService<ContainerAdapter>();

        }

        [TestInitialize]
        [TestCleanup]
        public async Task ClearContainer()
        {
            await Adapter.ClearContainerAsync();
        }

        private static ContainerAdapter Adapter = null!;


        [TestMethod]
        public async Task QueryOdata01()
        {
            var person1 = await Adapter.UpsertItemAsync(new Person
            {
                Id = "jay-d",
                FirstName = "John",
                LastName = "Doe"
            });
            var person2 = await Adapter.UpsertItemAsync(new Person
            {
                Id = "jane-d",
                FirstName = "Jane",
                LastName = "Doe"
            });

            var uriParser = new EdmModelBuilder()
                .AddEntity<Person>(nameof(Person.Id), "persons")
                .Build()
                .CreateUriParser("https://api.company.com/persons?$filter=id eq 'jay-d'");

            var query = uriParser.CreateQueryDefinition();
            var persons = await Adapter.EnumItemsAsync<Person>(query).ToListAsync();
            Assert.AreEqual(1, persons.Count);
            Assert.AreEqual(person1.Resource.Id, persons.First().Id);
        }

        [TestMethod]
        public async Task QueryOdata02()
        {
            var emp1 = await Adapter.UpsertItemAsync(new Employee
            {
                EmployeeId = "007",
                FirstName = "James",
                LastName = "Bond"
            });

            var emp2 = await Adapter.UpsertItemAsync(new Employee
            {
                EmployeeId = "M",
                LastName = "Messervy",
                FirstName = "Miles"
            });

            var uriParser = new EdmModelBuilder()
                .AddEntity<Employee>(nameof(Employee.Id), "employees")
                .Build()
                .CreateUriParser("https://company.com/api/employees?$filter=employeeId eq '007'");
        }
    }

}
