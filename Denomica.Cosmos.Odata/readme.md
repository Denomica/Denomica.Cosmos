# Denomica.Cosmos.OData

A library that exposes functionality for converting OData parameters into a [`QueryDefinition`](https://docs.microsoft.com/dotnet/api/microsoft.azure.cosmos.querydefinition) object for Cosmos DB.

## Documentation

The documentation for this library is available on the [library wiki](https://github.com/Denomica/Denomica.Cosmos/wiki).

## Versions

### v2.0.2

- Fixed a problem with filtering on properties that are typed as `DateOnly` structs. Prior to this fix, then were incorrectly handled as `DateTime` properties, which resulted in the time component being a part of the filter expression, which would fail to properly match items, expecially when using the `equals` operator.

### v2.0.1

- Fixed an issue where sorting on properties with reserved keywords caused an error. In this update, all field names are now property formatted as `c["PropertyName"]`, ensuring compatibility with Cosmos DB reserved keywords.

### v2.0.0

- Updated reference from [`Denomica.Cosmos.Extensions`](https://www.nuget.org/packages/Denomica.Cosmos.Extensions) to [`Denomica.Cosmos`](https://www.nuget.org/packages/Denomica.Cosmos). The new reference provides more or less the same functionality, but the namespaces have changed, so this must be considered as a breaking change. Hence the major version is updated.
- Fixed a bug that caused a query error when an entity property name matched a Cosmos DB reserved keyword.

#### Known Issues in v2.0.0

- Still problems with filtering on properties that are types as enumerations. This will be fixed in a future version.

### v1.0.0

- The first stable release of the `Denomica.Cosmos.Odata` library.
