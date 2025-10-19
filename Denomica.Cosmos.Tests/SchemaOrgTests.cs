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
    public class SchemaOrgTests
    {
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var provider = context.CreateServiceProvider();
            Adapater = provider.GetRequiredService<ContainerAdapter>();
        }

        [TestInitialize]
        [TestCleanup]
        public async Task ClearContainer()
        {
            await Adapater.ClearContainerAsync();
        }

        private static ContainerAdapter Adapater = null!;


    }
}
