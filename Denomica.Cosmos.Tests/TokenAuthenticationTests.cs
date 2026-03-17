using Azure.Identity;
using Denomica.Cosmos.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.Cosmos.Tests
{
    [TestClass]
    public class TokenAuthenticationTests
    {
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            ConnectionOptions = new CosmosConnectionOptions
            {
                ConnectionString = $"{context.Properties["connectionString"]}",
                DatabaseId = $"{context.Properties["databaseId"]}",
                ContainerId = $"{context.Properties["containerId"]}"
            };
        }

        private const string ConnectionString1Name = "connectionString";
        private const string ConnectionString2Name = "connectionString2";
        private const string ConnectionString3Name = "connectionString3";

        public TestContext TestContext { get; set; } = default!;

        private static CosmosConnectionOptions ConnectionOptions { get; set; } = default!;


        [TestMethod]
        public void ConfigureTokenAuth01()
        {
            this.SetConnectionOptions(ConnectionString2Name);

            var provider = new ServiceCollection()
                .AddCosmosServices()
                .AddTokenCredential<DefaultAzureCredential>()
                .WithContainerAdapter((opt, sp) =>
                {
                    opt.ConnectionString = ConnectionOptions.ConnectionString;
                    opt.DatabaseId = ConnectionOptions.DatabaseId;
                    opt.ContainerId = ConnectionOptions.ContainerId;
                })
                .Services

                .BuildServiceProvider();

            var client = provider.GetService<CosmosClient>();
            Assert.IsNotNull(client);
        }

        [TestMethod]
        public void ConfigureTokenAuth02()
        {
            this.SetConnectionOptions(ConnectionString3Name);

            var provider = new ServiceCollection()
                .AddCosmosServices()
                .AddDefaultTokenCredential()
                .WithContainerAdapter((opt, sp) =>
                {
                    opt.ConnectionString = ConnectionOptions.ConnectionString;
                    opt.DatabaseId = ConnectionOptions.DatabaseId;
                    opt.ContainerId = ConnectionOptions.ContainerId;
                })
                .Services

                .BuildServiceProvider();

            var client = provider.GetService<CosmosClient>();
            Assert.IsNotNull(client);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
        public void ConfigureTokenAuth03()
        {
            this.SetConnectionOptions(ConnectionString3Name);

            var provider = new ServiceCollection()
                .AddCosmosServices()
                .WithContainerAdapter((opt, sp) =>
                {
                    opt.ConnectionString = ConnectionOptions.ConnectionString;
                    opt.DatabaseId = ConnectionOptions.DatabaseId;
                    opt.ContainerId = ConnectionOptions.ContainerId;
                })
                .Services

                .BuildServiceProvider();

            var client = provider.GetService<CosmosClient>();
            Assert.IsNotNull(client);
        }



        private void SetConnectionOptions(string connectionStringName)
        {
            ConnectionOptions = new CosmosConnectionOptions
            {
                ConnectionString = $"{this.TestContext.Properties[connectionStringName]}",
                DatabaseId = $"{this.TestContext.Properties["databaseId"]}",
                ContainerId = $"{this.TestContext.Properties["containerId"]}"
            };
        }
    }
}
