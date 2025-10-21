using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Schema.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.Cosmos.Tests
{
    public static class TestExtensions
    {

        public static IServiceProvider CreateServiceProvider(this TestContext context)
        {
            var connectionString = $"{context.Properties["connectionString"]}";
            var databaseId = $"{context.Properties["databaseId"]}";
            var containerId = $"{context.Properties["containerId"]}";

            var provider = new ServiceCollection()
                .AddCosmosServices()
                .WithContainerAdapter((opt, sp) =>
                {
                    opt.ConnectionString = connectionString;
                    opt.DatabaseId = databaseId;
                    opt.ContainerId = containerId;
                })
                .Services

                .BuildServiceProvider();

            return provider;
        }

        public static async Task ClearContainerAsync(this ContainerAdapter adapter)
        {
            var tasks = new List<Task>();
            var items = adapter.EnumItemsAsync(new QueryDefinition("select c.id,c.partition from c"));
            await foreach (var item in items)
            {
                PartitionKey partition = PartitionKey.None;
                var id = item["id"] as string ?? throw new NullReferenceException();
                if (item.TryGetValue("partition", out var partitionValue))
                {
                    partition = adapter.CreatePartitionKey(partitionValue);
                }
                tasks.Add(adapter.DeleteItemAsync(id, partition, throwIfNotfound: false));
            }

            await Task.WhenAll(tasks);

            var errors = from x in tasks where !x.IsCompletedSuccessfully select x;
            if (errors.Any())
            {
                throw new Exception("Unable to clear all items from container.");
            }

            var count = await adapter.GetContainerCountAsync();
            Assert.AreEqual(0, count);
        }

        public static async Task<int> GetContainerCountAsync(this ContainerAdapter adapter)
        {
            var query = new QueryDefinition("select count(1) as itemCount from c");
            var result = await adapter.FirstOrDefaultAsync<ItemCountEntity>(query);// .QueryItemsAsync(query).ToListAsync();
            return result?.ItemCount ?? 0;
        }

    }
}
