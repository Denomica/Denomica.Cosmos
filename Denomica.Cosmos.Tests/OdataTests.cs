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

        }
    }
}
