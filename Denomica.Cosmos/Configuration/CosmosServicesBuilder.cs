using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Runtime.InteropServices;
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
        /// <remarks>
        /// <para>
        /// This method registers the necessary services for interacting with a Cosmos DB
        /// container, including <see cref="CosmosClient"/>, <see cref="Container"/>, and <see
        /// cref="ContainerAdapter"/>.
        /// </para>
        /// <para>
        /// The <see cref="CosmosClient"/> that is created supports both account key authentication and authentication
        /// with a token credential.
        /// </para>
        /// <para>
        /// The provided <paramref name="configureOptions"/> delegate is used to configure
        /// the connection options required to establish the Cosmos DB connection.
        /// </para>
        /// <para>
        /// If the <see cref="CosmosConnectionOptions.ConnectionString"/> is is a valid <see cref="Uri"/>, or if the
        /// connection string does not contain an <c>AccountKey</c> property, then this method will create a <see cref="CosmosClient"/>
        /// using a token credential. For this, you need to register an implementation of <see cref="ITokenCredentialProvider"/> in the 
        /// service collection, which will be used to obtain the token credential for authentication. You can register a token credential 
        /// provider using the <see cref="AddTokenCredentialProvider{TTokenCredentialProvider}()"/> method on this builder.
        /// </para>
        /// </remarks>
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

                    Func<Uri, CosmosClient> getTokenAuthCosmosClient = uri =>
                    {
                        var tokenCredential = sp.GetRequiredService<TokenCredential>();
                        return new CosmosClient(uri.ToString(), tokenCredential, clientOptions);
                    };
                    if(Uri.TryCreate(connectionOptions.ConnectionString, UriKind.Absolute, out var uri))
                    {
                        // If the connection string is a valid URI, we assume it's an endpoint and use token credential authentication
                        return getTokenAuthCosmosClient(uri);
                    }
                    else
                    {
                        var csBuilder = new DbConnectionStringBuilder
                        {
                            ConnectionString = connectionOptions.ConnectionString
                        };

                        if(csBuilder.ContainsKey("AccountKey"))
                        {
                            // If the connection string contains an AccountKey, we can use it directly to create the CosmosClient
                            return new CosmosClient(connectionOptions.ConnectionString, clientOptions);
                        }
                        else if(csBuilder.ContainsKey("AccountEndpoint"))
                        {
                            // If the connection string does not contain an AccountKey but contains an AccountEndpoint, we can use the endpoint to create a CosmosClient with token credential authentication
                            var endpoint = new Uri(csBuilder["AccountEndpoint"].ToString());
                            return getTokenAuthCosmosClient(endpoint);
                        }
                        else
                        {
                            throw new ConfigurationErrorsException("The Cosmos DB connection string is invalid.");
                        }
                    }
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


        /// <summary>
        /// Adds a default token credential to the service collection, which is used for authenticating with Cosmos DB when 
        /// using token credential authentication. The default token credential is an instance of <see cref="DefaultAzureCredential"/>.
        /// </summary>
        public CosmosServicesBuilder AddDefaultTokenCredential()
        {
            return this.AddTokenCredential<DefaultAzureCredential>();
        }

        /// <summary>
        /// Adds a token credential of the specified type to the service collection, which is used for authenticating 
        /// with Cosmos DB when using token credential authentication.
        /// </summary>
        /// <typeparam name="TTokenCredential">The type of token credential to add.</typeparam>
        public CosmosServicesBuilder AddTokenCredential<TTokenCredential>() where TTokenCredential : TokenCredential
        {
            this.Services.AddSingleton<TokenCredential, TTokenCredential>();
            return this;
        }

        /// <summary>
        /// Adds a token credential of the specified type to the service collection, which is used for authenticating 
        /// with Cosmos DB when using token credential authentication.
        /// </summary>
        /// <typeparam name="TTokenCredential">The type of token credential to add.</typeparam>
        /// <param name="config">A delegate that configures the token credential.</param>
        public CosmosServicesBuilder AddTokenCredential<TTokenCredential>(Action<IServiceProvider, TokenCredential> config) where TTokenCredential : TokenCredential
        {
            this.Services.AddSingleton<TokenCredential>(sp =>
            {
                var credential = ActivatorUtilities.CreateInstance<TTokenCredential>(sp);
                config(sp, credential);
                return credential;
            });
            return this;
        }

        /// <summary>
        /// Adds a token credential of the specified type to the service collection, which is used for authenticating with
        /// Cosmos DB when using token credential authentication. The token credential is created using the provided factory delegate.
        /// </summary>
        /// <typeparam name="TTokenCredential">The type of token credential to add.</typeparam>
        /// <param name="config">A delegate that configures the token credential.</param>
        public CosmosServicesBuilder AddTokenCredential<TTokenCredential>(Func<IServiceProvider, TTokenCredential> config) where TTokenCredential : TokenCredential
        {
            this.Services.AddSingleton<TokenCredential>(sp => config.Invoke(sp));
            return this;
        }

    }
}
