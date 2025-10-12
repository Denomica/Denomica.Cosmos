using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Text.Json;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Denomica.Cosmos.Tests
{
    [TestClass]
    public class DataAccessTests
    {

        [ClassInitialize]
        public static void ClassInit(TestContext context)
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

            Adapter = provider.GetRequiredService<ContainerAdapter>();
        }

        private static ContainerAdapter Adapter = null!;


        [TestInitialize]
        [TestCleanup]
        public async Task ClearContainer()
        {
            var tasks = new List<Task>();
            var items = Adapter.EnumItemsAsync(new QueryDefinition("select c.id,c.partition from c"));
            await foreach(var item in items)
            {
                PartitionKey partition = PartitionKey.None;
                var id = item["id"] as string ?? throw new NullReferenceException();
                if(item.TryGetValue("partition", out var partitionValue))
                {
                    partition = Adapter.CreatePartitionKey(partitionValue);
                }
                tasks.Add(Adapter.DeleteItemAsync(id, partition, throwIfNotfound: false));
            }

            await Task.WhenAll(tasks);

            var errors = from x in tasks where !x.IsCompletedSuccessfully select x;
            if(errors.Any())
            {
                throw new Exception("Unable to clear all items from container.");
            }

            var count = await this.GetContainerCountAsync();
            Assert.AreEqual(0, count);
        }



        [TestMethod]
        [Description("Bulk loads the container ensuring that no HTTP 429 is returned.")]
        public async Task BulkLoad01()
        {
            int itemCount = 5000;
            var upsertTasks = new List<Task<ItemResponse<Dictionary<string, object>>>>();
            for(var i = 0; i < itemCount; i++)
            {
                var doc = new Dictionary<string, object>
                {
                    { "Id", Guid.NewGuid() },
                    { "FirstName", $"First Name {i}" },
                    { "LastName", $"Last name {i}" },
                    { "EmployeeNumber", i }
                };

                upsertTasks.Add(Adapter.UpsertItemAsync(doc));
            }

            await Task.WhenAll(upsertTasks);
            var errors = from x in upsertTasks where !x.IsCompletedSuccessfully select x.Result;
            Assert.AreEqual(0, errors.Count(), "There must be no error responses.");

            var count = await this.GetContainerCountAsync();
            Assert.AreEqual(itemCount, count);
        }



        [TestMethod]
        [Description("Deletes an item from the container.")]
        public async Task Delete01()
        {
            object partition = Guid.NewGuid();

            var item = new Item1 { Partition = $"{partition}" };
            var response = await Adapter.UpsertItemAsync(item);

            await Adapter.DeleteItemAsync(item.Id, $"{partition}");

            var count = await this.GetContainerCountAsync();
            Assert.AreEqual(0, count, "All items must have been deleted from the container.");
        }



        [TestMethod]
        [Description("Stores a few items and attempts to get the first item matching a query and specifies a derived type to return the result as.")]
        public async Task GetFirstOrDefault01()
        {
            var itm1 = new ChildItem1 { DisplayName = "Child #1" };
            var itm2 = new ChildItem1 { DisplayName = "Child #2" };

            await Adapter.UpsertItemAsync(itm1);
            await Adapter.UpsertItemAsync(itm2);

            var itm1b = await Adapter.FirstOrDefaultAsync<Item1>(x => x.Where(x => x.Id == itm1.Id), typeof(ChildItem1)) as ChildItem1;
            var itm2b = await Adapter.FirstOrDefaultAsync<Item1>(x => x.Where(x => x.Id == itm2.Id), typeof(ChildItem1)) as ChildItem1;

            Assert.IsNotNull(itm1b);
            Assert.IsNotNull(itm2b);

            Assert.AreEqual(itm1.DisplayName, itm1b.DisplayName);
            Assert.AreEqual(itm2.DisplayName, itm2b.DisplayName);
        }


        [TestMethod]
        [Description("Stores items in the database and queries for them using a QueryDefinition object.")]
        public async Task Query01()
        {
            var partitionCount = 50;
            var itemCount = 10;

            var upsertTasks = new List<Task>();
            for(var p = 0; p < partitionCount; p++)
            {
                for(var i = 0; i < itemCount; i++)
                {
                    upsertTasks.Add(Adapter.UpsertItemAsync(new { Id = Guid.NewGuid(), Partition = p, Value = p * i }));
                }
            }

            await Task.WhenAll(upsertTasks);

            for(var p = 0; p < partitionCount; p++)
            {
                var query = new QueryDefinitionBuilder()
                    .AppendQueryText("select * from c")
                    .AppendQueryText(" where")

                    .AppendQueryText(" c[\"partition\"] = @partition")
                    .WithParameter("@partition", p)

                    .AppendQueryText(" order by c[\"value\"] desc")
                    .Build();

                var items = await Adapter.EnumItemsAsync(query).ToListAsync();
                Assert.AreEqual(itemCount, items.Count);

                var prevValue = p * itemCount + 1;
                foreach(var item in items)
                {
                    int val;
                    item.TryGetValue("value", out var valueObj);
                    int.TryParse($"{valueObj}", out val);

                    Assert.IsTrue(prevValue >= val);
                    prevValue = val;
                }
            }
        }

        [TestMethod]
        [Description("Creates a couple of items and queries for them with Linq expressions.")]
        public async Task Query02()
        {
            var partitions = new List<string>();
            var partitionCount = 100;
            var itemCountPerPartition = 20;

            var upsertTasks = new List<Task>();
            for(var p = 0; p < partitionCount; p++)
            {
                var partition = $"{Guid.NewGuid()}";
                partitions.Add(partition);

                for(var i = 0; i < itemCountPerPartition; i++)
                {
                    upsertTasks.Add(Adapter.UpsertItemAsync(new Item1 { Id = Guid.NewGuid().ToString(), Index = i, Partition = partition }));
                }
            }

            await Task.WhenAll(upsertTasks);

            foreach (var p in partitions)
            {
                var query = Adapter.Container
                    .GetItemLinqQueryable<Item1>()
                    .Where(x => x.Partition == p)
                    .OrderBy(x => x.Index)
                    .ToQueryDefinition();

                var items = await Adapter.EnumItemsAsync<Item1>(query).ToListAsync();
                Assert.AreEqual(itemCountPerPartition, items.Count);
                CollectionAssert.AllItemsAreUnique(new List<int>(from x in items select x.Index));
            }
        }

        [TestMethod]
        [Description("Creates a few items and queries data using Linq expressions.")]
        public async Task Query03()
        {
            var ids = new string[] {"1", "2", "3"};

            int i = 0;
            foreach(var id in ids)
            {
                await Adapter.UpsertItemAsync(new Item1 { Id = id, Index = i, Partition = "p" });
                i++;
            }

            var item1 = await Adapter.FirstOrDefaultAsync<Item1>(x => x.Where(x => x.Partition == "p"));
            Assert.IsNotNull(item1);

            var item2 = await Adapter.FirstOrDefaultAsync<Item1>(x => Adapter.Container.GetItemLinqQueryable<Item1>().OrderByDescending(x => x.Index));
            Assert.IsNotNull(item2);
            Assert.AreEqual(ids.Last(), item2.Id);
        }

        [TestMethod]
        [Description("Creates a lot of items, queries for them in a specific order and ensures that they are returned in the right order.")]
        public async Task Query04()
        {
            int count = 1000;
            var items = new List<Item1>();

            var upsertTasks = new List<Task>();
            for(var i = 0; i < count; i++)
            {
                var item = new Item1 { Id = Guid.NewGuid().ToString(), Index = i };
                items.Add(item);
                upsertTasks.Add(Adapter.UpsertItemAsync(item));
            }

            await Task.WhenAll(upsertTasks);
            Assert.AreEqual(count, items.Count);

            var query = from x in Adapter.Container.GetItemLinqQueryable<Item1>() orderby x.Index select x;
            await foreach(var item in Adapter.EnumItemsAsync(query))
            {
                var firstItem = items.First();
                items.RemoveAt(0);

                Assert.AreEqual(firstItem.Id, item.Id);
            }

            Assert.AreEqual(0, items.Count);
        }

        [TestMethod]
        [Description("Create a set of items in the database and use the query method that supports paging with continuation tokens.")]
        public async Task Query05()
        {
            var itemCount = 1000;
            int pageItemCount = 20; // Must produce the same number of items on each page.
            var source = new List<ChildItem1>();
            for(var i = 0; i < itemCount; i++)
            {
                var item = new ChildItem1 { Index = i, DisplayName = $"Item #{i}", Partition = Guid.NewGuid().ToString() };
                source.Add(item);
                await Adapter.UpsertItemAsync(item);
            }

            var results = new List<ChildItem1>();
            string? continuationToken = null;
            var query = new QueryDefinition("select * from c order by c.id");
            do
            {
                var result = await Adapter.PageItemsAsync<ChildItem1>(query, continuationToken, requestOptions: new QueryRequestOptions { MaxItemCount = pageItemCount });
                continuationToken = result.ContinuationToken;

                Assert.AreEqual(pageItemCount, result.Items.Count());
                results.AddRange(result.Items);
            } while (continuationToken?.Length > 0);

            CollectionAssert.AreEqual(source.OrderBy(x => x.Id).ToList(), results);
        }

        [TestMethod]
        [Description("Creates a set of items and queries for the results using the generic query method.")]
        public async Task Query06()
        {
            var itemCount = 100;
            var pageItemCount = 10;
            var source = new List<Item1>();

            for(var i = 0; i < itemCount; i++)
            {
                var item = new Item1 { Index = i };
                source.Add(await Adapter.UpsertItemAsync(item));
            }

            source = source.OrderBy(x => x.Id).ToList();
            var results = new List<Item1>();
            var query = from x in Adapter.Container.GetItemLinqQueryable<Item1>() orderby x.Id select x;

            PageResult<Item1>? result = await Adapter.PageItemsAsync<Item1>(query, requestOptions: new QueryRequestOptions { MaxItemCount = pageItemCount });
            while(result?.Items?.Count() > 0)
            {
                Assert.AreEqual(result.Items.Count(), pageItemCount);
                Assert.IsFalse(result.Items.Any(x => results.Any(y => y.Id == x.Id)), "Results must not include previously returned results.");
                results.AddRange(result.Items);
                result = await result.GetNextPageAsync();
            }

            CollectionAssert.AreEqual(source, results);
        }

        [TestMethod]
        [Description("Create an uneven number of items and page through the results and make sure that each page contains items.")]
        public async Task Query07()
        {
            int count = 57;
            for(var i = 0; i < count; i++)
            {
                await Adapter.UpsertItemAsync(new { Id = Guid.NewGuid() });
            }

            int pageCount = 0, itemCount = 0, maxItemCount = 11;
            var result = await Adapter.PageItemsAsync(new QueryDefinition("select * from c"), requestOptions: new QueryRequestOptions { MaxItemCount = maxItemCount });
            while(result.HasItems)
            {
                Assert.IsTrue(result.Items.Count() <= maxItemCount);

                pageCount++;
                itemCount += result.Items.Count();
                result = await result.GetNextPageAsync();
            }

            Assert.AreEqual(6, pageCount);
            Assert.AreEqual(count, itemCount);
        }

        [TestMethod]
        [Description("Queries the underlying container for data.")]
        public async Task Query08()
        {
            var upserted = await Adapter.UpsertItemAsync(new ContainerItem { });

            var result = await Adapter.PageItemsAsync(new QueryDefinition("select * from c"));
            Assert.AreEqual(1, result.Items.Count());
        }

        [TestMethod]
        [Description("Query the underlying container for all data.")]
        public async Task Query09()
        {
            int count = 78;
            var taskList = new List<Task>();
            for(var i = 0; i < count; i++)
            {
                taskList.Add(Adapter.UpsertItemAsync(new ContainerItem { }));
            }
            await Task.WhenAll(taskList);

            var items = await Adapter.EnumItemsAsync(new QueryDefinition("select * from c"), requestOptions: new QueryRequestOptions { MaxItemCount = 10 }).ToListAsync();
            Assert.AreEqual(count, items.Count);
        }

        [TestMethod]
        [Description("Create more than 100 items and expect all of the items to be returned, even without specifying any request options.")]
        public async Task Query10()
        {
            int count = 243;
            var taskList = new List<Task>();
            for (var i = 0; i < count; i++)
            {
                taskList.Add(Adapter.UpsertItemAsync(new ContainerItem { }));
            }
            await Task.WhenAll(taskList);
            var items = await Adapter.EnumItemsAsync<ContainerItem>(new QueryDefinition("select * from c")).ToListAsync();
            Assert.AreEqual(count, items.Count);
        }

        [TestMethod]
        [Description("Store one item in the underlying container, and make sure that it returned exactly the same after querying.")]
        public async Task Query11()
        {
            var result = await Adapter.UpsertItemAsync(new ChildItem1 { DisplayName = "Foo Bar", Index = 123 });
            var expected = result.Resource;
            var items = await Adapter.EnumItemsAsync<ChildItem1>(x => x.Where(xx => xx.Id == result.Resource.Id)).ToListAsync();
            Assert.AreEqual(1, items.Count);

            var item = items.First();
            Assert.AreEqual(expected.Id, item.Id);
            Assert.AreEqual(expected.Partition, item.Partition);
            Assert.AreEqual(expected.Index, item.Index);
            Assert.AreEqual(expected.DisplayName, item.DisplayName);
        }

        [TestMethod]
        [Description("Store one item in the container, and query it and make sure that it is identical to the stored.")]
        public async Task Query12()
        {
            var result = await Adapter.UpsertItemAsync(new ContainerItem());
            var items = await Adapter.EnumItemsAsync<ContainerItem>(x => x.Where(xx => xx.Id == result.Resource.Id)).ToListAsync();
            Assert.AreEqual(1, items.Count);

            var item = items.First();
            Assert.AreEqual(result.Resource.Id, item.Id);
            Assert.AreEqual(result.Resource.Partition, item.Partition);
        }



        [TestMethod]
        [Description("Inserts one item and checks the item count.")]
        public async Task SelectCount01()
        {
            var response = await Adapter.UpsertItemAsync(new { Id = Guid.NewGuid() });
            var query = new QueryDefinition("select count(1) from c");
            var result = await Adapter.EnumItemsAsync<Dictionary<string, JsonElement>>(query).ToListAsync();
            Assert.AreEqual(1, result.Count);
            var count = result.First()["$1"];
            Assert.AreEqual(1, count.GetInt32());
        }



        [TestMethod]
        [Description("Upserts a document into the database and expects successful result.")]
        public async Task Upsert01()
        {
            var doc = new Dictionary<string, object>
            {
                { "Id", Guid.NewGuid() },
                { "Partition", Guid.NewGuid() }
            };

            var response = await Adapter.UpsertItemAsync(doc);
            Assert.IsNotNull(response);
        }

        [TestMethod]
        [Description("Upserts an item typed as a parent type, but assumes that it will be returned as the actual type.")]
        public async Task Upsert02()
        {
            var displayName = "Child Item";
            Item1 item = new ChildItem1 { DisplayName = displayName, Partition = Guid.NewGuid().ToString() };
            var upserted = await Adapter.UpsertItemAsync(item, new PartitionKey(item.Partition));
            Assert.IsNotNull(upserted);
            Assert.IsTrue(upserted.Resource is ChildItem1);
            var item2 = upserted.Resource as ChildItem1;
            Assert.IsNotNull(item2);
            Assert.AreEqual(displayName, item2.DisplayName);
        }

        [TestMethod]
        [Description("Upsert an item as a parent type without a partition key, and expect that the item will be returned as the actual type.")]
        public async Task Upsert03()
        {
            var name = "Foo Bar";
            Item1 item = new ChildItem1 { DisplayName = name };
            var upserted = await Adapter.UpsertItemAsync(item);
            var item2 = upserted.Resource as ChildItem1;

            Assert.IsNotNull(item2);
        }


        private async Task<int> GetContainerCountAsync()
        {
            var query = new QueryDefinition("select count(1) as itemCount from c");
            var result = await Adapter.FirstOrDefaultAsync<ItemCountEntity>(query);// .QueryItemsAsync(query).ToListAsync();
            return result?.ItemCount ?? 0;
        }
    }

    public class ContainerItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public object Partition { get; set; } = Guid.NewGuid().ToString();
    }

    public class Item1
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Partition { get; set; } = Guid.NewGuid().ToString();

        public int Index { get; set; }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is Item1 && this.ToString() == obj.ToString();
        }

        public override string ToString()
        {
            return $"{this.Id}|{this.Partition}|{this.Index}";
        }
    }

    public class ChildItem1 : Item1
    {
        public string? DisplayName { get; set; }

        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            return obj is ChildItem1 && this.ToString() == obj.ToString();
        }

        public override string ToString()
        {
            return $"{base.ToString()}|{this.DisplayName}";
        }
    }

    public class ItemCountEntity
    {
        public int ItemCount { get; set; }
    }
}