using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Denomica.Cosmos.SchemaOrg.Model;

namespace Denomica.Cosmos.SchemaOrg.Services
{
    /// <summary>
    /// This interface defines the service contract for service implementations that
    /// are used to build instances of <see cref="SchemaOrgEnvelope"/> objects from
    /// given JSON input.
    /// </summary>
    public interface ISchemaOrgEnvelopeBuilder
    {
        /// <summary>
        /// Builds instances of <see cref="SchemaOrgEnvelope"/> from the provided JSON string asynchronously.
        /// </summary>
        /// <param name="json">
        /// The JSON string to build <see cref="SchemaOrgEnvelope"/> instances from.
        /// </param>
        IAsyncEnumerable<SchemaOrgEnvelope> BuildAsync(string json);

        /// <summary>
        /// Builds instances of <see cref="SchemaOrgEnvelope"/> from the provided JSON stream asynchronously.
        /// </summary>
        /// <param name="jsonStream">
        /// The stream containing the JSON to build <see cref="SchemaOrgEnvelope"/> instances from.
        /// </param>
        /// <returns></returns>
        IAsyncEnumerable<SchemaOrgEnvelope> BuildAsync(Stream jsonStream);
    }
}
