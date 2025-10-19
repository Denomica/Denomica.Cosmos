using Denomica.Cosmos.SchemaOrg.Model;
using Denomica.Text.Json;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Denomica.Cosmos.SchemaOrg.Services
{
    public sealed class SchemaOrgEnvelopeBuilder : SchemaOrgEnvelopeBuilderBase
    {
        public override async IAsyncEnumerable<SchemaOrgEnvelope> BuildAsync(Stream jsonStream, SchemaOrgEnvelopeBuilderOptions? options = null)
        {
            options ??= new SchemaOrgEnvelopeBuilderOptions();

            string json = string.Empty;
            using (var reader = new StreamReader(jsonStream))
            {
                json = await reader.ReadToEndAsync();
            }

            yield break;
        }
    }
}
