using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Denomica.Cosmos
{
    /// <summary>
    /// Provides an adapter for interacting with an Azure Cosmos DB container, enabling common operations such as
    /// querying, inserting, updating, and deleting items.
    /// </summary>
    /// <remarks>This class acts as a wrapper around the <see cref="Container"/> class, simplifying common
    /// operations and providing additional functionality such as upsert operations and partition key handling. It is
    /// designed to work with Azure Cosmos DB SDK and supports asynchronous operations for scalability.</remarks>
    public class ContainerAdapter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerAdapter"/> class with the specified dependency
        /// container.
        /// </summary>
        /// <param name="container">The dependency injection container used to resolve service dependencies. Cannot be <see langword="null"/>.</param>
        /// <param name="serializationOptions">Optional JSON serialization options to customize the serialization and deserialization behavior. If not provided, default options will be used.</param>
        public ContainerAdapter(Container container, JsonSerializerOptions? serializationOptions = null)
        {
            this.Container = container ?? throw new ArgumentNullException(nameof(container));
            this.SerializationOptions = serializationOptions ?? new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        /// <summary>
        /// Returns the underlying <see cref="Container"/> instance used for data operations.
        /// </summary>
        public Container Container { get; private set; }

        /// <summary>
        /// Gets the options used to configure JSON serialization.
        /// </summary>
        public JsonSerializerOptions SerializationOptions { get; private set; }



        /// <summary>
        /// Creates a <see cref="PartitionKey"/> object from the given value.
        /// </summary>
        /// <param name="value">The value to create the <see cref="PartitionKey"/> from.</param>
        /// <exception cref="InvalidCastException">
        /// The exception that is found if <paramref name="value"/> cannot be converted into a 
        /// value that can be used to create a <see cref="PartitionKey"/> object from.
        /// </exception>
        public PartitionKey CreatePartitionKey(object? value)
        {
            PartitionKey key = PartitionKey.Null;
            if (null != value)
            {
                if (value is string)
                {
                    key = new PartitionKey((string)value);
                }
                else if (value is Guid)
                {
                    key = new PartitionKey($"{value}");
                }
                else if (value is double)
                {
                    key = new PartitionKey((double)value);
                }
                else if (double.TryParse($"{value}", out double d))
                {
                    key = new PartitionKey(d);
                }
                else if (value is bool)
                {
                    key = new PartitionKey((bool)value);
                }
                else if (bool.TryParse($"{value}", out bool b))
                {
                    key = new PartitionKey(b);
                }
                else if (value is JsonElement)
                {
                    var elem = (JsonElement)value;
                    switch (elem.ValueKind)
                    {
                        case JsonValueKind.String:
                            key = new PartitionKey(elem.GetString());
                            break;

                        case JsonValueKind.Number:
                            if (elem.TryGetInt64(out var i))
                            {
                                key = new PartitionKey(i);
                            }
                            else if (elem.TryGetDouble(out var dd))
                            {
                                key = new PartitionKey(dd);
                            }
                            else
                            {
                                throw new InvalidCastException($"Cannot convert numeric value '{elem.GetRawText()}' to either integer or double.");
                            }
                            break;

                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            key = new PartitionKey(elem.GetBoolean());
                            break;

                        case JsonValueKind.Null:
                            key = PartitionKey.Null;
                            break;

                        default:
                            throw new InvalidCastException($"Cannot convert JsonElement whose value type is '{elem.ValueKind}' to a PartitionKey.");
                    }
                }
            }

            return key;
        }

        /// <summary>
        /// Deletes the item with the given <paramref name="id"/> and <paramref name="partition"/>.
        /// </summary>
        /// <param name="id">The ID of the item to delete.</param>
        /// <param name="partition">The partition of the item to delete.</param>
        /// <param name="throwIfNotfound">
        /// Specifies whether to throw an exception if the specified document is not
        /// found. Defaults to <c>true</c>.
        /// </param>
        public async Task DeleteItemAsync(string id, PartitionKey partition, bool throwIfNotfound = true)
        {
            var response = await this.Container.DeleteItemStreamAsync(id, partition);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Deletes the document with the given <paramref name="id"/> and <paramref name="partition"/>.
        /// </summary>
        /// <param name="id">The ID of the document to delete.</param>
        /// <param name="partition">The partition of the document to delete.</param>
        /// <param name="throwIfNotfound">
        /// Specifies whether to throw an exception if the specified document is not found. Defaults to <c>true</c>.
        /// </param>
        public Task DeleteItemAsync(string id, string? partition, bool throwIfNotfound = true)
        {
            return this.DeleteItemAsync(id, partition?.Length > 0 ? new PartitionKey(partition) : PartitionKey.None, throwIfNotfound: throwIfNotfound);
        }



        /// <summary>
        /// Asynchronously retrieves the first item from the query results, or <see langword="null"/> if no items are
        /// found.
        /// </summary>
        /// <remarks>This method executes the query with a maximum item count of 1 and returns the first
        /// result, if available.</remarks>
        /// <param name="query">The <see cref="QueryDefinition"/> that defines the query to execute.</param>
        /// <returns>A <see cref="JsonElement"/> representing the first item in the query results, or <see langword="null"/> if
        /// the query returns no items.</returns>
        public async Task<JsonElement?> FirstOrDefaultAsync(QueryDefinition query)
        {
            var result = await this.QueryItemsAsync(query, requestOptions: new QueryRequestOptions { MaxItemCount = 1 }).ToListAsync();
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Asynchronously retrieves the first item from the query results or <see langword="null"/> if no items are found.
        /// </summary>
        /// <remarks>
        /// This method executes the query with a maximum item count of 1, ensuring that at most one item is retrieved.
        /// </remarks>
        /// <typeparam name="TItem">The type of the item to retrieve. Must be a reference type.</typeparam>
        /// <param name="query">The <see cref="QueryDefinition"/> that defines the query to execute.</param>
        /// <param name="returnAs">
        /// An optional <see cref="Type"/> specifying the desired runtime type for the returned item. If specified, it MUST be a 
        /// subtype of <typeparamref name="TItem"/>. If not specified, the items are returned as the type <typeparamref name="TItem"/>.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the first item of type 
        /// <typeparamref name="TItem"/> from the query results, or <see langword="null"/> if no items are found.
        /// </returns>
        public async Task<TItem?> FirstOrDefaultAsync<TItem>(QueryDefinition query, Type? returnAs = null) where TItem : class
        {
            var result = await this.QueryItemsAsync<TItem>(query, returnAs: returnAs, requestOptions: new QueryRequestOptions { MaxItemCount = 1 }).ToListAsync();
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Asynchronously retrieves the first element of a queryable sequence, or a default value if the sequence is
        /// empty.
        /// </summary>
        /// <remarks>This method executes the query asynchronously and retrieves only the first item, if
        /// available. The query is shaped using the provided <paramref name="queryShaper"/> function, which allows
        /// customization of the query logic.</remarks>
        /// <typeparam name="TItem">The type of the elements in the queryable sequence. Must be a reference type.</typeparam>
        /// <param name="queryShaper">
        /// A function that shapes the query by applying additional filters, projections, or other transformations to
        /// the <see cref="IQueryable{T}"/> sequence.
        /// </param>
        /// <param name="returnAs">
        /// An optional <see cref="Type"/> specifying the desired runtime type for the returned item. If specified, it MUST be a 
        /// subtype of <typeparamref name="TItem"/>. If not specified, the items are returned as the type <typeparamref name="TItem"/>.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the first element of the shaped
        /// queryable sequence, or <see langword="null"/> if the sequence contains no elements.
        /// </returns>
        public async Task<TItem?> FirstOrDefaultAsync<TItem>(Func<IQueryable<TItem>, IQueryable<TItem>> queryShaper, Type? returnAs = null) where TItem : class
        {
            var requestOptions = new QueryRequestOptions { MaxItemCount = 1 };
            var shapedQuery = queryShaper(this.Container.GetItemLinqQueryable<TItem>(requestOptions: requestOptions));
            var result = await this.QueryItemsAsync<TItem>(x => shapedQuery, returnAs: returnAs, requestOptions: requestOptions).ToListAsync();
            return result.FirstOrDefault();
        }



        /// <summary>
        /// Executes a query against a data source and retrieves the results as a paginated feed.
        /// </summary>
        /// <remarks>This method is useful for executing paginated queries against a data source. If the
        /// query spans multiple pages,  use the continuation token from the <see cref="QueryResult{TItem}"/> to
        /// retrieve subsequent pages.</remarks>
        /// <typeparam name="TItem">The type of the items in the query result. Must be a reference type.</typeparam>
        /// <param name="query">The query to execute, represented as an <see cref="IQueryable{T}"/>.</param>
        /// <param name="continuationToken">An optional token used to retrieve the next set of results in a paginated query.  Pass <see
        /// langword="null"/> to start from the beginning of the query.</param>
        /// <param name="returnAs">An optional <see cref="Type"/> specifying the desired runtime type for the query results.  If <see
        /// langword="null"/>, the results will be returned as the type specified by <typeparamref name="TItem"/>.</param>
        /// <param name="requestOptions">Optional settings that specify additional options for the query, such as throughput or consistency level.
        /// Pass <see langword="null"/> to use the default options.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains a  <see
        /// cref="QueryResult{TItem}"/> object that includes the query results and a continuation token for retrieving 
        /// additional results, if available.</returns>
        public Task<QueryResult<TItem>> QueryFeedAsync<TItem>(IQueryable<TItem> query, string? continuationToken = null, Type? returnAs = null, QueryRequestOptions? requestOptions = null)
        {
            return this.QueryFeedAsync<TItem>(query.ToQueryDefinition(), continuationToken, returnAs: returnAs, requestOptions: requestOptions);
        }

        /// <summary>
        /// Executes a query against the underlying Cosmos DB container, and returns the results as a feed of items with an optional
        /// continuation token to be used to get the next set of results (feed).
        /// </summary>
        /// <typeparam name="TItem">The type of the items to be returned. Must be a reference type.</typeparam>
        /// <param name="query">The <see cref="QueryDefinition"/> that defines the query to execute.</param>
        /// <param name="continuationToken">
        /// An optional token used to continue a query from where it previously stopped. If null, the query starts from
        /// the beginning.
        /// </param>
        /// <param name="returnAs">
        /// An optional <see cref="Type"/> specifying the type to which the query results should be converted. If specified, the type
        /// must be a subtype of <typeparamref name="TItem"/>. If not specified, the results are returned as <typeparamref name="TItem"/>.
        /// </param>
        /// <param name="requestOptions">
        /// Optional <see cref="QueryRequestOptions"/> to configure the query execution, such as request limits or consistency levels.
        /// </param>
        /// <returns>
        /// A <see cref="QueryResult{TItem}"/> containing the query results, including the items, continuation token, 
        /// request charge, and status code.
        /// </returns>
        public async Task<QueryResult<TItem>> QueryFeedAsync<TItem>(QueryDefinition query, string? continuationToken = null, Type? returnAs = null, QueryRequestOptions? requestOptions = null)
        {
            QueryResult<TItem> result = new QueryResult<TItem>(this, query, returnAs: returnAs);
            var items = new List<TItem>();

            var iterator = this.Container.GetItemQueryIterator<JsonElement>(query, continuationToken, requestOptions: requestOptions);
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();

                result.ContinuationToken = response.ContinuationToken;
                result.RequestCharge = response.RequestCharge;
                result.StatusCode = response.StatusCode;

                foreach (var item in response)
                {
                    items.Add(this.Convert<TItem>(item, returnAs: returnAs));
                }

                result.Items = items;
            }

            return result;
        }



        /// <summary>
        /// Executes a query against a data source and asynchronously returns the results as a sequence of items.
        /// </summary>
        /// <remarks>This method supports asynchronous streaming using <see cref="IAsyncEnumerable{T}"/>.
        /// It is suitable for scenarios where large result sets need to be processed incrementally.</remarks>
        /// <typeparam name="TItem">The type of the items to be returned. Must be a reference type.</typeparam>
        /// <param name="query">The query to execute, represented as an <see cref="IQueryable{T}"/>. This defines the criteria for
        /// retrieving items.</param>
        /// <param name="returnAs">An optional <see cref="Type"/> specifying the type to which the results should be cast. If null, the results
        /// are returned as <typeparamref name="TItem"/>.</param>
        /// <param name="requestOptions">Optional settings that specify additional options for the query, such as consistency level or request
        /// limits.</param>
        /// <returns>An asynchronous sequence of items that match the query criteria. The sequence is streamed, and items are
        /// retrieved lazily as the caller enumerates.</returns>
        public async IAsyncEnumerable<TItem> QueryItemsAsync<TItem>(IQueryable<TItem> query, Type? returnAs = null, QueryRequestOptions? requestOptions = null) where TItem : class
        {
            var queryDef = query.ToQueryDefinition<TItem>();
            await foreach (var item in this.QueryItemsAsync<TItem>(queryDef, returnAs: returnAs, requestOptions: requestOptions))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Asynchronously queries items from the container using the specified query shaping function.
        /// </summary>
        /// <remarks>
        /// This method uses asynchronous streaming to retrieve items, making it suitable for processing
        /// large datasets without loading all items into memory at once. The query is shaped using the
        /// <paramref name="queryShaper"/> function, which allows for flexible filtering and projection.
        /// </remarks>
        /// <typeparam name="TItem">
        /// The type of the items to query.
        /// </typeparam>
        /// <param name="queryShaper">
        /// A function that shapes the query by applying additional filters, projections, or other LINQ operations. The
        /// function takes an <see cref="IQueryable{T}"/> representing the base query and returns a modified query.
        /// </param>
        /// <param name="returnAs">
        /// An optional <see cref="Type"/> specifying the desired runtime type for the returned items. If specified, it MUST be a 
        /// subtype of <typeparamref name="TItem"/>. If not specified, the items are returned as the type <typeparamref name="TItem"/>.
        /// </param>
        /// <param name="requestOptions">
        /// Optional settings for the query request, such as consistency level or maximum item count per page.
        /// </param>
        /// <returns>An asynchronous stream of items of type <typeparamref name="TItem"/> that match the query.</returns>
        public async IAsyncEnumerable<TItem> QueryItemsAsync<TItem>(Func<IQueryable<TItem>, IQueryable<TItem>> queryShaper, Type? returnAs = null, QueryRequestOptions? requestOptions = null) where TItem : class
        {
            var shapedQuery = queryShaper(this.Container.GetItemLinqQueryable<TItem>(requestOptions: requestOptions));
            await foreach(var item in this.QueryItemsAsync<TItem>(shapedQuery, returnAs: returnAs, requestOptions: requestOptions))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Executes a query against the data source and returns the results as an asynchronous stream of items.
        /// </summary>
        /// <remarks>This method uses asynchronous streaming to retrieve query results, which allows
        /// processing large result sets efficiently without loading all items into memory at once.</remarks>
        /// <typeparam name="TItem">The type of the items to return. Must be a reference type.</typeparam>
        /// <param name="query">The <see cref="QueryDefinition"/> that defines the query to execute.</param>
        /// <param name="returnAs">
        /// An optional <see cref="Type"/> to which the items should be converted. If specified, the type must be a subtype of 
        /// <typeparamref name="TItem"/>. If not specified, the items are returned as the type specified by <typeparamref name="TItem"/>.
        /// </param>
        /// <param name="requestOptions">Optional settings for the query request, such as consistency level or partition key.</param>
        /// <returns>An asynchronous stream of items of type <typeparamref name="TItem"/> that match the query.</returns>
        public async IAsyncEnumerable<TItem> QueryItemsAsync<TItem>(QueryDefinition query, Type? returnAs = null, QueryRequestOptions? requestOptions = null) where TItem : class
        {
            await foreach (var item in this.QueryItemsAsync(query, requestOptions: requestOptions))
            {
                yield return this.Convert<TItem>(item, returnAs: returnAs);
            }
        }

        /// <summary>
        /// Queries the underlying <see cref="Container"/> for items.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <param name="requestOptions">Optional query request options to customize the query execution.</param>
        /// <returns>Returns the results as an async enumerable collection.</returns>
        public async IAsyncEnumerable<JsonElement> QueryItemsAsync(QueryDefinition query, QueryRequestOptions? requestOptions = null)
        {
            string? continuationToken = null;

            do
            {
                var result = await this.QueryFeedAsync<JsonElement>(query, continuationToken: continuationToken, requestOptions: requestOptions);
                continuationToken = result.ContinuationToken;
                foreach (var item in result.Items)
                {
                    yield return item;
                }
            } while (continuationToken?.Length > 0);

            yield break;
        }



        /// <summary>
        /// Inserts or updates an item in the database container.
        /// </summary>
        /// <remarks>This method performs an "upsert" operation, which means it will insert the item if it
        /// does not already exist  or update it if it does. The operation is performed within the context of the
        /// specified partition key.</remarks>
        /// <param name="item">The item to be inserted or updated. The item must be a JSON-serializable object.</param>
        /// <param name="partitionKey">The partition key associated with the item. If <see langword="null"/>, the partition key will be inferred 
        /// from the item's properties if possible.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response from the database, 
        /// including the inserted or updated item and metadata about the operation.</returns>
        public Task<ItemResponse<object>> UpsertItemAsync(object item, PartitionKey? partitionKey = null)
        {
            return this.UpsertItemAsync<object>(item, partitionKey);
        }

        /// <summary>
        /// Inserts or updates an item in the Azure Cosmos DB container.
        /// </summary>
        /// <remarks>This method performs an upsert operation, which inserts the item if it does not exist
        /// or updates it if it already exists. The operation is performed within the specified partition key, if
        /// provided.</remarks>
        /// <typeparam name="TItem">The type of the item to be upserted. Must be a reference type.</typeparam>
        /// <param name="item">The item to be inserted or updated. Cannot be <see langword="null"/>.</param>
        /// <param name="partitionKey">The partition key associated with the item. If <see langword="null"/>, the default partition key will be
        /// used.</param>
        /// <returns>A <see cref="ItemResponse{T}"/> containing the result of the upsert operation, including the item and
        /// metadata such as the status code and request charge.</returns>
        /// <exception cref="CosmosException">Thrown if the upsert operation fails with a non-success status code (outside the range 200-299).</exception>
        public async Task<ItemResponse<TItem>> UpsertItemAsync<TItem>(TItem item, PartitionKey? partitionKey = null) where TItem : class
        {
            var response = await this.Container.UpsertItemAsync<TItem>(item, partitionKey: partitionKey);
            if((int)response.StatusCode < 200 || (int)response.StatusCode >= 300)
            {
                throw new CosmosException($"Upsert operation failed with status code {response.StatusCode}.", response.StatusCode, (int)response.StatusCode, response.ActivityId, response.RequestCharge);
            }

            return response;
        }



        private TItem Convert<TItem>(JsonElement element, Type? returnAs = null)
        {
            Type targetType = returnAs ?? typeof(TItem);
            TItem resultItem = default!;
            if (null == returnAs)
            {
                resultItem = JsonSerializer.Deserialize<TItem>(element, options: this.SerializationOptions);
            }
            else
            {
                resultItem = (TItem)JsonSerializer.Deserialize(element, returnAs, options: this.SerializationOptions);
            }

            return resultItem ?? throw new Exception($"Cannot convert given JSON element to type '{targetType.FullName}'.");
        }
    }
}
