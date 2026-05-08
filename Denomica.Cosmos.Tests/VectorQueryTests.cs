using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

namespace Denomica.Cosmos.Tests
{
    [TestClass]
    public class VectorQueryTests
    {
        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var provider = context.CreateServiceProvider();
            Adapter = provider.GetRequiredService<ContainerAdapter>();
        }

        private static ContainerAdapter Adapter = null!;

        [TestInitialize]
        [TestCleanup]
        public async Task ClearContainer()
        {
            await Adapter.ClearContainerAsync();
        }



        [TestMethod]
        public async Task QueryVector01()
        {
            int count = 100;
            int itemId = 50;

            for(int i = 0; i < count; i++)
            {
                var doc = new VectorDocument
                {
                    Id = i.ToString(),
                    Embedding = new Model.VectorEmbedding
                    {
                        Model = "test-model",
                        Vector = [1, 2, 3, i]
                    }
                };

                await Adapter.UpsertItemAsync(doc);
            }

            var queryVector = new float[] { 1, 2, 3, itemId };
            double minScore = 0.5;

            var query = Adapter.CreateQueryDefinitionBuilder()
                .AppendQueryText("select * from c")
                .AppendQueryText(" where c.type = @type")
                .AppendQueryText(" and VectorDistance(c.embedding.vector, @queryVector) >= @minScore")

                .WithParameter("@type", nameof(VectorDocument))
                .WithParameter("@queryVector", queryVector)
                .WithParameter("@minScore", minScore)

                .AppendQueryText(" order by VectorDistance(c.embedding.vector, @queryVector)")

                .Build();

            VectorDocument? result = null;
            int resultCount = 0;
            await foreach (var item in Adapter.EnumItemsAsync<VectorDocument>(query, requestOptions: new QueryRequestOptions { MaxItemCount = 1 }))
            {
                result = item;
                resultCount++;
            }

            Assert.AreEqual(1, resultCount);
            Assert.IsNotNull(result);
            Assert.AreEqual(itemId.ToString(), result.Id);
        }
    }
}
