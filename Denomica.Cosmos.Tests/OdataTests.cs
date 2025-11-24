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

        [TestMethod]
        public async Task QueryOdata03()
        {
            var period1 = await Adapter.UpsertItemAsync(new TimePeriod
            {
                Start = new DateOnly(2022, 1, 1),
                End = new DateOnly(2022, 12, 31)
            });
            var period2 = await Adapter.UpsertItemAsync(new TimePeriod
            {
                Start = new DateOnly(2023, 1, 1),
                End = new DateOnly(2023, 12, 31)
            });

            var query = new EdmModelBuilder()
                .AddEntity<TimePeriod>(nameof(TimePeriod.Id), "timeperiods")
                .Build()
                .CreateUriParser("https://api.company.com/timeperiods?$filter=end lt 2023-01-01")
                .CreateQueryDefinition();

            var periods = await Adapter.EnumItemsAsync<TimePeriod>(query).ToListAsync();
            Assert.AreEqual(1, periods.Count);
            Assert.AreEqual(period1.Resource.Id, periods.First().Id);
        }

        [TestMethod]
        public async Task QueryOdata04()
        {
            var period1 = await Adapter.UpsertItemAsync(new TimePeriod
            {
                Start = new DateOnly(2022, 1, 1),
                End = new DateOnly(2022, 12, 31)
            });
            var period2 = await Adapter.UpsertItemAsync(new TimePeriod
            {
                Start = new DateOnly(2023, 1, 1),
                End = new DateOnly(2023, 12, 31)
            });

            var query = new EdmModelBuilder()
                .AddEntity<TimePeriod>(nameof(TimePeriod.Id), "timeperiods")
                .Build()
                .CreateUriParser("https://api.company.com/timeperiods?$select=start,end&$filter=end lt 2023-01-01")
                .CreateQueryDefinition();

            var periods = await Adapter.EnumItemsAsync<Dictionary<string, object?>>(query).ToListAsync();
            Assert.AreEqual(1, periods.Count);
            var period = periods.First();
            Assert.AreEqual(2, period.Keys.Count);
            Assert.IsTrue(period.Keys.Contains("start"));
            Assert.IsTrue(period.Keys.Contains("end"));
        }

        [TestMethod]
        public async Task QueryOdata05()
        {
            var ci1 = await Adapter.UpsertItemAsync(new ContentItem
            {
                Title = "Item #1",
                Status = DocumentStatus.Draft
            });
            var ci2 = await Adapter.UpsertItemAsync(new ContentItem
            {
                Title = "Item #2",
                Status = DocumentStatus.Approved
            });

            var model = new EdmModelBuilder()
                .AddEntity<ContentItem>(nameof(ContentItem.Id), "contentitems")
                .Build();

            var query = model
                .CreateUriParser("https://api.company.com/contentitems?$filter=status eq -1")
                .CreateQueryDefinition();

            var contentItems = await Adapter.EnumItemsAsync<ContentItem>(query).ToListAsync();
            Assert.AreEqual(1, contentItems.Count);
            Assert.AreEqual(ci2.Resource.Id, contentItems.First().Id);
        }

        
    }

}
