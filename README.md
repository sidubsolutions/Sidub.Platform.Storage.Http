# Sidub Platform - HTTP Storage

This repository contains the HTTP storage module for the Sidub Platform. It
provides connectors and handlers that allow the storage framework to interact
with various HTTP storage and API endpoints.

## Main Components
This library simply provides the connectors and handlers for HTTP data
services. The `Sidub.Platform.Authentication.Http` library provides
authentication capabilities and credentials for this data service.

### Registering an OData Service
To connect to an OData data source, register it as a metadata service using
the `StorageServiceReference` and `ODataStorageConnector` classes.

```csharp
serviceCollection.AddSidubPlatform(serviceProvider =>
{
    var metadataService = new InMemoryServiceRegistry();

    var dataService = new StorageServiceReference("MyApi");
    var dataServiceConnector = new ODataStorageConnector("https://my.api.com/");

    metadataService.RegisterServiceReference(dataService, dataServiceConnector);

    return metadataService;
});
```

### Registering an Azure Table Service
To connect to an OData data source, register it as a metadata service using
the `StorageServiceReference` and `TableStorageConnector` classes.

```csharp
serviceCollection.AddSidubPlatform(serviceProvider =>
{
    var metadataService = new InMemoryServiceRegistry();

    var dataService = new StorageServiceReference("MyApi");
    var dataServiceConnector = new TableStorageConnector("https://my.api.com/");

    metadataService.RegisterServiceReference(dataService, dataServiceConnector);

    return metadataService;
});
```

### Registering an Azure Blob Service
To connect to an OData data source, register it as a metadata service using
the `StorageServiceReference` and `BlobStorageConnector` classes.

```csharp
serviceCollection.AddSidubPlatform(serviceProvider =>
{
    var metadataService = new InMemoryServiceRegistry();

    var dataService = new StorageServiceReference("MyApi");
    var dataServiceConnector = new BlobStorageConnector("https://my.api.com/");

    metadataService.RegisterServiceReference(dataService, dataServiceConnector);

    return metadataService;
});
```

### Registering an Azure Queue Service
To connect to an OData data source, register it as a metadata service using
the `StorageServiceReference` and `QueueStorageConnector` classes.

```csharp
serviceCollection.AddSidubPlatform(serviceProvider =>
{
    var metadataService = new InMemoryServiceRegistry();

    var dataService = new StorageServiceReference("MyApi");
    var dataServiceConnector = new QueueStorageConnector("https://my.api.com/");

    metadataService.RegisterServiceReference(dataService, dataServiceConnector);

    return metadataService;
});
```

To interact with the HTTP data service, use any of the functionality defined
within the storage framework, simply passing the storage service reference
associated with the Gremlin connector.

## License
This project is dual-licensed under the AGPL v3 or a proprietary license. For
details, see [https://sidub.ca/licensing](https://sidub.ca/licensing) or the
LICENSE.txt file.