# Denomica.Cosmos.Model

A library that defines model classes that are designed to be stored in a Cosmos DB container.

> This library is the successor to the [`Denomica.Cosmos.Extensions.Model`](https://www.nuget.org/packages/Denomica.Cosmos.Extensions.Model/) library.

## Main Features

This library defines a set of model classes that are designed to be stored in a Cosmos DB container.

- `DocumentBase`: The base class for all documents. Defines the following properties:
	- `Id`: The unique identifier of the document. Defaults to a new Guid.
	- `Type`: The name of the document type. Defaults to the name of the class.
- `TimestampedDocumentBase`: Inherits from `DocumentBase` and adds `Created` and `Modified` properties.
- `SyntheticPartitionKeyDocumentBase`: Inherits from `TimestampedDocumentBase` and adds a `Partition` property that supports the [synthetic partition key](https://learn.microsoft.com/azure/cosmos-db/nosql/synthetic-partition-keys) pattern.

## Version Highlights

Major improvements in various versions.

### v1.0.0-beta.2

- A few adjustments and fixes to package documentation.

### v1.0.0-beta.1

- Initial release of the Denomica.Cosmos.Model library.