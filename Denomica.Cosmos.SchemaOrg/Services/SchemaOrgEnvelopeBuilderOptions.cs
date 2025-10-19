using System;
using System.Collections.Generic;
using System.Text;
using Denomica.Cosmos.SchemaOrg.Model;

namespace Denomica.Cosmos.SchemaOrg.Services
{
    /// <summary>
    /// Options for service implementations that implement the <see cref="ISchemaOrgEnvelopeBuilder"/> interface.
    /// </summary>
    public sealed class SchemaOrgEnvelopeBuilderOptions
    {
        /// <summary>
        /// Controls whether the builder should omit storing the raw schema.org object content in the 
        /// <see cref="SchemaOrgEnvelope.Content"/> property.
        /// </summary>
        public bool OmitContent { get; set; }
    }
}
