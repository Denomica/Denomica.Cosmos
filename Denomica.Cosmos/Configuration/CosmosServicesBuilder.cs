using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Denomica.Cosmos.Configuration
{
    /// <summary>
    /// Provides a builder for configuring Cosmos-related services in an application.
    /// </summary>
    /// <remarks>This class is used to configure and register services related to Cosmos DB within the
    /// provided <see cref="IServiceCollection"/>. It is typically used in application startup to set up Cosmos-specific
    /// dependencies.</remarks>
    public class CosmosServicesBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosServicesBuilder"/> class.
        /// </summary>
        /// <param name="services">The collection of service descriptors to configure Cosmos-related services.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is <see langword="null"/>.</exception>
        public CosmosServicesBuilder(IServiceCollection services)
        {
            this.Services = services ?? throw new ArgumentNullException(nameof(services));
        }


        /// <summary>
        /// Gets the collection of service descriptors used to configure dependency injection.
        /// </summary>
        public IServiceCollection Services { get; private set; }



        /// <summary>
        /// Configures the <see cref="ContainerAdapter"/> instance for your application with the specified connection options
        /// and all services that the <see cref="ContainerAdapter"/> instance requires.
        /// </summary>
        /// <remarks>This method registers the necessary services for interacting with a Cosmos DB
        /// container, including <see cref="CosmosClient"/>, <see cref="Container"/>, and <see
        /// cref="ContainerAdapter"/>. The provided <paramref name="configureOptions"/> delegate is used to configure
        /// the connection options required to establish the Cosmos DB connection.</remarks>
        /// <param name="configureOptions">A delegate that configures the <see cref="CosmosConnectionOptions"/> using the provided <see
        /// cref="IServiceProvider"/>.</param>
        /// <returns>The current <see cref="CosmosServicesBuilder"/> instance, allowing for method chaining.</returns>
        public CosmosServicesBuilder WithContainerAdapter(Action<CosmosConnectionOptions, IServiceProvider> configureOptions)
        {
            this.Services
                .AddOptions<CosmosConnectionOptions>()
                .Configure<IServiceProvider>(configureOptions)
                .Services

                .AddSingleton<CosmosClient>(sp =>
                {
                    var connectionOptions = sp.GetRequiredService<IOptions<CosmosConnectionOptions>>().Value;
                    var clientOptions = sp.GetRequiredService<IOptions<CosmosClientOptions>>().Value;
                    return new CosmosClient(connectionOptions.ConnectionString, clientOptions);
                })
                .AddSingleton<Container>(sp =>
                {
                    var connectionOptions = sp.GetRequiredService<IOptions<CosmosConnectionOptions>>().Value;
                    var client = sp.GetRequiredService<CosmosClient>();
                    return client.GetContainer(connectionOptions.DatabaseId, connectionOptions.ContainerId);
                })
                .AddSingleton<ContainerAdapter>(sp =>
                {
                    var container = sp.GetRequiredService<Container>();
                    var jsonOptions = sp.GetRequiredService<IOptions<JsonSerializerOptions>>().Value;
                    return new ContainerAdapter(container, serializationOptions: jsonOptions);
                })
                ;

            return this;
        }

        /// <summary>
        /// Configures the <see cref="CosmosClientOptions"/> for the Cosmos DB client.
        /// </summary>
        /// <remarks>Use this method to customize the <see cref="CosmosClientOptions"/> for the Cosmos DB
        /// client, such as setting connection policies, retry options, or other client-specific
        /// configurations.</remarks>
        /// <param name="configureOptions">A delegate that configures the <see cref="CosmosClientOptions"/> using the provided <see
        /// cref="IServiceProvider"/>.</param>
        /// <returns>The current <see cref="CosmosServicesBuilder"/> instance, allowing for method chaining.</returns>
        public CosmosServicesBuilder WithCosmosClientOptions(Action<CosmosClientOptions, IServiceProvider> configureOptions)
        {
            this.Services
                .AddOptions<CosmosClientOptions>()
                .Configure<IServiceProvider>(configureOptions)
                ;

            return this;
        }

        /// <summary>
        /// Configures JSON serialization options for the application.
        /// </summary>
        /// <remarks>This method allows customization of JSON serialization behavior by modifying the <see
        /// cref="JsonSerializerOptions"/> used throughout the application. The provided delegate is invoked to apply
        /// the desired configuration.</remarks>
        /// <param name="configureOptions">A delegate that configures an instance of <see cref="JsonSerializerOptions"/> using the provided <see
        /// cref="IServiceProvider"/>.</param>
        /// <returns>The current instance of <see cref="CosmosServicesBuilder"/> to allow for method chaining.</returns>
        public CosmosServicesBuilder WithJsonSerializationOptions(Action<JsonSerializerOptions, IServiceProvider> configureOptions)
        {
            this.Services
                .AddOptions<JsonSerializerOptions>()
                .Configure<IServiceProvider>(configureOptions)
                ;

            return this;
        }

    }
}
