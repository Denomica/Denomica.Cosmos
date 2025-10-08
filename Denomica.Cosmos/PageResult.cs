using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Denomica.Cosmos
{
    /// <summary>
    /// Represents the result of a paged query operation, including the items retrieved, continuation token, and request
    /// charge.
    /// </summary>
    /// <remarks>
    /// The <see cref="PageResult{T}"/> class provides access to the items produced by a query, as well as metadata such as 
    /// the continuation token for retrieving additional results and the request charge for the operation. Use the 
    /// <see cref="GetNextPageAsync"/> method to retrieve the next set of results if a continuation token is available.
    /// </remarks>
    /// <typeparam name="T">
    /// The type of the items returned by the query.
    /// </typeparam>
    public class PageResult<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PageResult{T}"/> class with the specified container, query
        /// definition, query options, and optional return type.
        /// </summary>
        /// <param name="adapter">The <see cref="ContainerAdapter"/> responsible for producing the <see cref="PageResult{T}"/></param>
        /// <param name="query">The query definition that specifies the query to be executed. Cannot be <see langword="null"/>.</param>
        /// <param name="returnAs">The optional type to which the query results will be cast. If <see langword="null"/>, the default type is
        /// used.</param>
        /// <param name="requestOptions">The optional request options for the query execution.</param>
        /// <exception cref="ArgumentNullException">
        /// The exception that is thrown when required parameters are <see langword="null"/>.
        /// </exception>
        internal PageResult(ContainerAdapter adapter, QueryDefinition query, Type? returnAs = null, QueryRequestOptions? requestOptions = null)
        {
            this.Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            this.Query = query ?? throw new ArgumentNullException(nameof(query));
            this.ReturnAs = returnAs;
            this.RequestOptions = requestOptions;
        }

        private readonly ContainerAdapter Adapter;
        private readonly QueryDefinition Query;
        private readonly Type? ReturnAs;
        private readonly QueryRequestOptions? RequestOptions;

        /// <summary>
        /// The items that were produced by executing a query.
        /// </summary>
        public IEnumerable<T> Items { get; internal set; } = Enumerable.Empty<T>();

        /// <summary>
        /// The continuation token that is used to get the next set of items.
        /// </summary>
        public string? ContinuationToken { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the collection contains any items.
        /// </summary>
        public bool HasItems => this.Items.Any();

        /// <summary>
        /// The request charge for producing the items in <see cref="Items"/>.
        /// </summary>
        public double RequestCharge { get; internal set; }

        /// <summary>
        /// Gets the HTTP status code returned by the response.
        /// </summary>
        public System.Net.HttpStatusCode StatusCode { get; internal set; }

        /// <summary>
        /// Returns the next page of result set. If there are no more results, the method returns an empty result set.
        /// </summary>
        /// <remarks>
        /// This method returns an empty result set if the <see cref="ContinuationToken"/> is <see langword="null"/>, 
        /// indicating that there are no more results to retrieve.
        /// </remarks>
        public async Task<PageResult<T>> GetNextPageAsync()
        {
            if(null != this.ContinuationToken)
            {
                return await this.Adapter.PageItemsAsync<T>(
                    this.Query, 
                    this.ContinuationToken,
                    returnAs: this.ReturnAs, 
                    requestOptions: this.RequestOptions);
            }

            return new PageResult<T>(this.Adapter, this.Query) { StatusCode = System.Net.HttpStatusCode.NoContent };
        }
    }
}
