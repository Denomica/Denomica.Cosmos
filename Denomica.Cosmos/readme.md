# Denomica.Cosmos

A library that facilitates working with data in Azure Cosmos DB.

> This library is the successor to the [Denomica.Cosmos.Extensions](https://www.nuget.org/packages/Denomica.Cosmos.Extensions/) library.

## Main Features

This library build on the Azure Cosmos DB SDK ([Microsoft.Azure.Cosmos](https://www.nuget.org/packages/Microsoft.Azure.Cosmos)) and provides the following main features.

1. Introduces the `ContainerAdapter` class, which provides additional ways for working with data in a Cosmos DB container.
	- Handles the Cosmos DB feed iterator and returns results in batches with support for retrieving the following batch.
	- Provides methods that return `IAsyncEnumerable<T>` that allows iterating over all result items without having to deal with continuation tokens or feed iterators.
	- Improves on subtype handling when inserting or updating items.
2. Provides the `QueryDefinitionBuilder` class that simplifies building `QueryDefinition` instances that you use to query data in Cosmos DB with.
3. Supports Dependency Injection with the `CosmosServicesBuilder` class that faciliates adding options and services related to working with data in Cosmos DB.

## Related Packages

The following packages can be used together with this library to provide additional capabilities.

- [`Denomica.Cosmos.Model`](https://www.nuget.org/packages/Denomica.Cosmos.Model)

## Version Highlights

Major improvements in various versions of this library.

### v1.0.0-beta.1

- Initial release of the `Denomica.Cosmos` library.