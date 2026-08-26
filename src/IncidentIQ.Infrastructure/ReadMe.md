# IncidentIQ.Infrastructure

`IncidentIQ.Infrastructure` contains the concrete implementations used by the Application layer to communicate with external systems.

It is responsible for persistence, messaging, Azure SDK integration, and other technical concerns that should not live inside the Domain or Application projects.

---

## Responsibilities

Current responsibilities include:

- Cosmos DB client configuration.
- Incident persistence.
- Runbook persistence.
- Local Cosmos initialization.
- Azure Service Bus client configuration.
- Sending `AnalyseIncident` commands.
- Azure authentication through `DefaultAzureCredential` where configured.
- Dependency injection registration for infrastructure services.

---

## High-Level Structure

```text
IncidentIQ.Infrastructure/
├── Messaging/
│   ├── AzureServiceBusIncidentAnalysisQueue.cs
│   └── ServiceBusOptions.cs
│
├── Persistence/
│   └── Cosmos/
│       ├── CosmosOptions.cs
│       ├── CosmosInitializer.cs
│       ├── CosmosIncidentRepository.cs
│       ├── CosmosRunbookRepository.cs
│       └── persistence documents
│
└── DependencyInjection.cs
```

---

## Cosmos DB

IncidentIQ uses the native Azure Cosmos DB SDK.

Current Cosmos persistence includes:

```text
IncidentIQ Database
├── Incidents
└── Runbooks
```

Both containers currently use:

```text
Partition key: /id
```

Repository implementations map between Domain entities and Cosmos persistence documents.

The local `CosmosInitializer` creates development containers when running against the local emulator.

Azure infrastructure is provisioned through Bicep instead.

---

## Service Bus

Infrastructure provides the implementation of:

```text
IIncidentAnalysisQueue
```

using Azure Service Bus.

The API creates an `AnalyseIncidentCommand`, while Infrastructure serialises and sends it to:

```text
analyse-incident
```

The Application layer therefore does not need to know that Azure Service Bus is being used.

---

## Authentication

Infrastructure supports two common development modes:

```text
Docker Compose
→ emulator credentials / connection strings
```

```text
Azure
→ DefaultAzureCredential
→ Managed Identity or developer Azure identity
```

This allows the same application code to work against local emulators and Azure-hosted services.

---

## Design Approach

Infrastructure is the technical edge of the application:

- Application defines abstractions.
- Infrastructure implements those abstractions.
- Azure SDK types remain outside Domain and Application where practical.
- External-service configuration is bound through options classes.
- Long-lived SDK clients are registered and reused through dependency injection.
