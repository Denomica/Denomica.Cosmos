using Denomica.Cosmos.SchemaOrg.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Denomica.Cosmos.SchemaOrg.Services
{
    /// <summary>
    /// A base class for implementations of <see cref="SchemaOrgEnvelopeBuilderBase"/>.
    /// </summary>
    public abstract class SchemaOrgEnvelopeBuilderBase : ISchemaOrgEnvelopeBuilder
    {
        /// <inheritdoc/>
        public async IAsyncEnumerable<SchemaOrgEnvelope> BuildAsync(string json, SchemaOrgEnvelopeBuilderOptions? options = null)
        {
            using(var strm = new MemoryStream())
            {
                using (var writer = new StreamWriter(strm))
                {
                    await writer.WriteAsync(json);
                    await writer.FlushAsync();
                    strm.Position = 0;

                    await foreach (var envelope in BuildAsync(strm))
                    {
                        yield return envelope;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public abstract IAsyncEnumerable<SchemaOrgEnvelope> BuildAsync(Stream jsonStream, SchemaOrgEnvelopeBuilderOptions? options = null);

    }
}
