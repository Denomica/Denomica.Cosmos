using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Denomica.Text.Json;
using System.IO;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Denomica.Cosmos
{
    /// <summary>
    /// Provides an adapter for interacting with an Azure Cosmos DB container, enabling common operations such as
    /// querying, inserting, updating, and deleting items.
    /// </summary>
    /// <remarks>
    /// This class acts as a wrapper around the <see cref="Container"/> class, simplifying common
    /// operations and providing additional functionality.
    /// </remarks>
    public class ContainerAdapter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerAdapter"/>.
        /// </summary>
        /// <param name="container">
        /// Requred. The Cosmos DB container to use in the current <see cref="ContainerAdapter"/>.
        /// </param>
        /// <param name="serializationOptions">
        /// Optional. the JSON serialization options to customize the serialization and deserialization behavior. 
        /// If not provided, default options will be used.
        /// </param>
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
            if (throwIfNotfound)
            {
                var item = await this.FirstOrDefaultAsync(id, partition);
                if (null == item)
                {
                    throw new CosmosException("Specified item was fount found.", HttpStatusCode.NotFound, 0, string.Empty, 0);
                }
            }

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
        /// Retrieves the first item that matches the given <paramref name="query"/>, or <see langword="null"/>
        /// if no matching item is found.
        /// </summary>
        /// <remarks>This method executes the query with a maximum item count of 1 and returns the first
        /// result, if available.</remarks>
        /// <param name="query">The <see cref="QueryDefinition"/> that defines the query to execute.</param>
        /// <returns>A <see cref="JsonElement"/> representing the first item in the query results, or <see langword="null"/> if
        /// the query returns no items.</returns>
        public async Task<Dictionary<string, object?>?> FirstOrDefaultAsync(QueryDefinition query)
        {
            var result = await this.EnumItemsAsync(query, requestOptions: new QueryRequestOptions { MaxItemCount = 1 }).ToListAsync();
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Retrieves the first item that matches the given <paramref name="query"/>, or <see langword="null"/>
        /// if no matching item is found.
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
        /// Returns the first item that matches the quer or <see langword="null"/> if no matching item is found.
        /// </returns>
        public async Task<TItem?> FirstOrDefaultAsync<TItem>(QueryDefinition query, Type? returnAs = null) where TItem : class
        {
            var result = await this.EnumItemsAsync<TItem>(query, returnAs: returnAs, requestOptions: new QueryRequestOptions { MaxItemCount = 1 }).ToListAsync();
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Retrieves the first item that matches the query returned by <paramref name="queryShaper"/>, or 
        /// <see langword="null"/> if no matching item is found.
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
        /// Returns the first item that matches the quer or <see langword="null"/> if no matching item is found.
        /// </returns>
        public async Task<TItem?> FirstOrDefaultAsync<TItem>(Func<IQueryable<TItem>, IQueryable<TItem>> queryShaper, Type? returnAs = null) where TItem : class
        {
            var requestOptions = new QueryRequestOptions { MaxItemCount = 1 };
            var shapedQuery = queryShaper(this.Container.GetItemLinqQueryable<TItem>(requestOptions: requestOptions));
            var result = await this.EnumItemsAsync<TItem>(x => shapedQuery, returnAs: returnAs, requestOptions: requestOptions).ToListAsync();
            return result.FirstOrDefault();
        }

        /// <summary>
        /// Retrieves the first item from the container that matches the specified <paramref name="id"/> and <paramref
        /// name="partitionKey"/>.
        /// </summary>
        /// <param name="id">The unique identifier of the item to retrieve.</param>
        /// <param name="partitionKey">The partition key associated with the item.</param>
        /// <returns>
        /// A dictionary representing the item's properties if the item is found and the operation is successful;
        /// otherwise, <see langword="null"/>.
        /// </returns>
        public async Task<Dictionary<string, object?>?> FirstOrDefaultAsync(string id, PartitionKey partitionKey)
        {
            var response = await this.Container.ReadItemAsync<Dictionary<string, object?>>(id, partitionKey);
            if(response.StatusCode == HttpStatusCode.OK)
            {
                return response.Resource;
            }
            return null;
        }



        /// <summary>
        /// Executes a query against the underlying container and retrieves a paged result set with an optional continuation token.
        /// </summary>
        /// <typeparam name="TItem">The type of the items in the query result. Must be a reference type.</typeparam>
        /// <param name="query">The query to execute, represented as an <see cref="IQueryable{T}"/>.</param>
        /// <param name="continuationToken">An optional token used to retrieve the next set of results in a paginated query.  Pass <see
        /// langword="null"/> to start from the beginning of the query.</param>
        /// <param name="returnAs">An optional <see cref="Type"/> specifying the desired runtime type for the query results.  If <see
        /// langword="null"/>, the results will be returned as the type specified by <typeparamref name="TItem"/>.</param>
        /// <param name="requestOptions">Optional settings that specify additional options for the query, such as throughput or consistency level.
        /// Pass <see langword="null"/> to use the default options.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. The task result contains a  <see
        /// cref="PageResult{TItem}"/> object that includes the query results and a continuation token for retrieving 
        /// additional results, if available.</returns>
        public Task<PageResult<TItem>> PageItemsAsync<TItem>(IQueryable<TItem> query, string? continuationToken = null, Type? returnAs = null, QueryRequestOptions? requestOptions = null)
        {
            return this.PageItemsAsync<TItem>(query.ToQueryDefinition(), continuationToken, returnAs: returnAs, requestOptions: requestOptions);
        }

        /// <summary>
        /// Executes a query against the underlying container and retrieves a paged result set with an optional continuation token.
        /// </summary>
        /// <typeparam name="TItem">The type of the items to be returned.</typeparam>
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
        /// A <see cref="PageResult{TItem}"/> containing the query results, including the items, continuation token, 
        /// request charge, and status code.
        /// </returns>
        public async Task<PageResult<TItem>> PageItemsAsync<TItem>(QueryDefinition query, string? continuationToken = null, Type? returnAs = null, QueryRequestOptions? requestOptions = null)
        {
            PageResult<TItem> result = new PageResult<TItem>(this, query, returnAs: returnAs, requestOptions: requestOptions);
            var items = new List<TItem>();

            var feedResult = await this.PageItemsAsync(query, continuationToken: continuationToken, requestOptions: requestOptions);
            result.ContinuationToken = feedResult.ContinuationToken;
            result.RequestCharge = feedResult.RequestCharge;
            result.StatusCode = feedResult.StatusCode;

            foreach(var item in feedResult.Items)
            {
                items.Add(this.Convert<TItem>(item, returnAs: returnAs));
            }

            result.Items = items;
            return result;
        }

        /// <summary>
        /// Executes a query against the underlying container and retrieves a paged result set with an optional continuation token.
        /// </summary>
        /// <param name="query">The <see cref="QueryDefinition"/> that defines the query to execute.</param>
        /// <param name="continuationToken">
        /// An optional token used to continue a query from where it left off in a previous execution. Pass <see langword="null"/>
        /// to start a new query.
        /// </param>
        /// <param name="requestOptions">
        /// Optional <see cref="QueryRequestOptions"/> to configure the query execution, such as setting  limits on
        /// throughput or defining partition keys.
        /// </param>
        /// <returns>
        /// A <see cref="PageResult{T}"/> containing the query results as a collection of dictionaries,  where each
        /// dictionary represents an item with its properties as key-value pairs. The result  also includes metadata
        /// such as the continuation token, status code, and request charge.
        /// </returns>
        public async Task<PageResult<Dictionary<string, object?>>> PageItemsAsync(QueryDefinition query, string? continuationToken = null, QueryRequestOptions? requestOptions = null)
        {
            PageResult<Dictionary<string, object?>> result = new PageResult<Dictionary<string, object?>>(this, query, requestOptions: requestOptions);
            var items = new List<Dictionary<string, object?>>();
            var iterator = this.Container.GetItemQueryIterator<Dictionary<string, object?>>(query, continuationToken, requestOptions: requestOptions);
            
            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                
                result.ContinuationToken = response.ContinuationToken;
                result.StatusCode = response.StatusCode;
                result.RequestCharge = response.RequestCharge;

                foreach (var item in response)
                {
                    items.Add(item);
                }
                result.Items = items;
            }
            return result;
        }



        /// <summary>
        /// Executes a query against a data source and asynchronously returns the results as a sequence of items.
        /// </summary>
        /// <remarks>
        /// This method supports asynchronous enumeration using <see cref="IAsyncEnumerable{T}"/>. It allows you
        /// to enumerate through all items that match the given <paramref name="query"/>.
        /// </remarks>
        /// <typeparam name="TItem">The type of the items to be returned. Must be a reference type.</typeparam>
        /// <param name="query">
        /// The query to execute, represented as an <see cref="IQueryable{T}"/>. This defines the criteria for
        /// retrieving items.
        /// </param>
        /// <param name="returnAs">
        /// An optional <see cref="Type"/> specifying the type to which the query results should be converted. If specified, the type
        /// must be a subtype of <typeparamref name="TItem"/>. If not specified, the results are returned as <typeparamref name="TItem"/>.
        /// </param>
        /// <param name="requestOptions">
        /// Optional settings that specify additional options for the query, such as consistency level or request limits.
        /// </param>
        /// <returns>
        /// An asynchronous sequence of items that match the query criteria. The sequence is streamed, and items are
        /// retrieved lazily as the caller enumerates.
        /// </returns>
        public async IAsyncEnumerable<TItem> EnumItemsAsync<TItem>(IQueryable<TItem> query, Type? returnAs = null, QueryRequestOptions? requestOptions = null) where TItem : class
        {
            var queryDef = query.ToQueryDefinition<TItem>();
            await foreach (var item in this.EnumItemsAsync<TItem>(queryDef, returnAs: returnAs, requestOptions: requestOptions))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Asynchronously queries items from the container using the query returned by <paramref name="queryShaper"/>.
        /// </summary>
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
        /// <returns>
        /// Returns <see cref="IAsyncEnumerable{T}"/> that allows you to asynchronously enumerate through the items that match
        /// the query returned by <paramref name="queryShaper"/>.
        /// </returns>
        public async IAsyncEnumerable<TItem> EnumItemsAsync<TItem>(Func<IQueryable<TItem>, IQueryable<TItem>> queryShaper, Type? returnAs = null, QueryRequestOptions? requestOptions = null) where TItem : class
        {
            var shapedQuery = queryShaper(this.Container.GetItemLinqQueryable<TItem>(requestOptions: requestOptions));
            await foreach(var item in this.EnumItemsAsync<TItem>(shapedQuery, returnAs: returnAs, requestOptions: requestOptions))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Executes a <paramref name="query"/> against the underlying container and returns the results as an asynchronous stream of items.
        /// </summary>
        /// <typeparam name="TItem">The type of the items to return. Must be a reference type.</typeparam>
        /// <param name="query">The <see cref="QueryDefinition"/> that defines the query to execute.</param>
        /// <param name="returnAs">
        /// An optional <see cref="Type"/> to which the items should be converted. If specified, the type must be a subtype of 
        /// <typeparamref name="TItem"/>. If not specified, the items are returned as the type specified by <typeparamref name="TItem"/>.
        /// </param>
        /// <param name="requestOptions">Optional settings for the query request, such as consistency level or partition key.</param>
        /// <returns>An asynchronous stream of items of type <typeparamref name="TItem"/> that match the query.</returns>
        public async IAsyncEnumerable<TItem> EnumItemsAsync<TItem>(QueryDefinition query, Type? returnAs = null, QueryRequestOptions? requestOptions = null) where TItem : class
        {
            await foreach (var item in this.EnumItemsAsync(query, requestOptions: requestOptions))
            {
                yield return this.Convert<TItem>(item, returnAs: returnAs);
            }
        }

        /// <summary>
        /// Queries the underlying <see cref="Container"/> for items that match <paramref name="query"/>.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <param name="requestOptions">
        /// Optional query request options to customize the query execution.
        /// </param>
        /// <returns>
        /// Returns the results as an async enumerable collection.
        /// </returns>
        public async IAsyncEnumerable<Dictionary<string, object?>> EnumItemsAsync(QueryDefinition query, QueryRequestOptions? requestOptions = null)
        {
            string? continuationToken = null;

            do
            {
                var result = await this.PageItemsAsync(query, continuationToken: continuationToken, requestOptions: requestOptions);
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
        /// <remarks>
        /// This method performs an "upsert" operation, which means it will insert the item if it
        /// does not already exist  or update it if it does.
        /// </remarks>
        /// <param name="item">The item to be inserted or updated. The item must be a JSON-serializable object.</param>
        /// <param name="partitionKey">The partition key associated with the item. If <see langword="null"/>, the partition key will be inferred 
        /// from the item's properties if possible.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the response from the database, 
        /// including the inserted or updated item and metadata about the operation.
        /// </returns>
        public Task<ItemResponse<object>> UpsertItemAsync(object item, PartitionKey? partitionKey = null)
        {
            return this.UpsertItemAsync<object>(item, partitionKey);
        }

        /// <summary>
        /// Inserts or updates an item in the underlying Azure Cosmos DB container.
        /// </summary>
        /// <remarks>
        /// This method performs an upsert operation, which inserts the item if it does not exist
        /// or updates it if it already exists.
        /// </remarks>
        /// <typeparam name="TItem">The type of the item to be upserted. Must be a reference type.</typeparam>
        /// <param name="item">The item to be inserted or updated. Cannot be <see langword="null"/>.</param>
        /// <param name="partitionKey">The partition key associated with the item. If <see langword="null"/>, the default partition key will be
        /// used.</param>
        /// <param name="requestOptions">Optional. The options for the item request.</param>
        /// <returns>
        /// A <see cref="ItemResponse{T}"/> containing the result of the upsert operation, including the item and
        /// metadata such as the status code and request charge.
        /// </returns>
        /// <exception cref="CosmosException">Thrown if the upsert operation fails with a non-success status code (outside the range 200-299).</exception>
        public async Task<ItemResponse<TItem>> UpsertItemAsync<TItem>(TItem item, PartitionKey? partitionKey = null, ItemRequestOptions? requestOptions = null) where TItem : class
        {
            ItemResponse<TItem> response = null!;

            response = await this.Container.UpsertItemAsync<TItem>(item, partitionKey: partitionKey, requestOptions: requestOptions);
            if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 300)
            {
                throw new CosmosException($"Upsert operation failed with status code {response.StatusCode}.", response.StatusCode, (int)response.StatusCode, response.ActivityId, response.RequestCharge);
            }

            if (item.GetType() != typeof(TItem) && null != partitionKey)
            {
                // If the item is not exactly the type specified in TItem, it means that it is a subtype.
                // In that case, we need to retrieve the item from the container, because we might not
                // have the full updated item in the response from the Upsert method call, becaue the
                // item is a subtype of TItem.
                var sourceItem = JsonUtil.CreateDictionary(item);
                if(sourceItem.TryGetValue("id", out var idObj) && idObj is string id && id.Length > 0)
                {
                    var dictionary = await this.FirstOrDefaultAsync(id, partitionKey.Value);
                    if(null != dictionary)
                    {
                        response = new UpsertItemResponse<TItem>(response, this.Convert<TItem>(dictionary, returnAs: item.GetType()));
                    }
                }
            }

            return response;
        }



        private TItem Convert<TItem>(Dictionary<string, object?> source, Type? returnAs = null)
        {
            Type targetType = returnAs ?? typeof(TItem);
            TItem resultItem = default!;
            if (null == returnAs)
            {
                resultItem = JsonSerializer.Deserialize<TItem>(JsonSerializer.Serialize(source, options: this.SerializationOptions), options: this.SerializationOptions);
            }
            else
            {
                resultItem = (TItem)JsonSerializer.Deserialize(JsonSerializer.Serialize(source, options: this.SerializationOptions), returnAs, options: this.SerializationOptions);
            }

            return resultItem ?? throw new Exception($"Cannot convert given dictionary to type '{targetType.FullName}'.");
        }

    }
}
