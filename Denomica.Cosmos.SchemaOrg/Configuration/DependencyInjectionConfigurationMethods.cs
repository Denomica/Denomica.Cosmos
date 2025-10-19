using Denomica.Cosmos.SchemaOrg.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides extension methods for configuring dependency injection services related to Schema.org functionality.
    /// </summary>
    public static class DependencyInjectionConfigurationMethods
    {

        public static IServiceCollection AddSchemaOrgEnvelopeBuilder(this IServiceCollection services)
        {
            return services.AddSingleton<SchemaOrgEnvelopeBuilderBase, SchemaOrgEnvelopeBuilder>();
        }
    }
}
