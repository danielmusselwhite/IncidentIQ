# IncidentIQ.Infrastructure

`IncidentIQ.Infrastructure` contains the concrete implementations used by the Application layer to communicate with external systems.

It is responsible for persistence, messaging, Azure SDK integration, and other technical concerns that should not live inside the Domain or Application projects.

---

## Responsibilities

Current responsibilities include:

- Cosmos DB client configuration.
- Incident persistence.
- Atomic Incident + analysis outbox persistence.
- Runbook persistence.
- Local Cosmos initialization.
- Azure Service Bus client configuration.
- Sending `AnalyseIncidentCommand` messages.
- Azure authentication through `DefaultAzureCredential` where configured.
- Dependency injection registration for Infrastructure services.

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
│       ├── CosmosIncidentSubmissionStore.cs
│       ├── CosmosRunbookRepository.cs
│       └── Documents/
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
├── Runbooks
└── ChangeFeedLeases
```

The `Runbooks` container uses: `Partition key: /id`

The `ChangeFeedLeases` container uses: `Partition key: /id`

The `Incidents` container uses: `Partition key: /incidentId`

The `Incidents` container stores both Incident documents and analysis outbox documents.

Both document types share the same `incidentId`, which allows an Incident update/create and its associated outbox message to be persisted atomically using a Cosmos transactional batch.

Repository and persistence implementations map between Domain/Application models and Cosmos persistence documents.

The local `CosmosInitializer` creates development containers when running against the local emulator.

Azure infrastructure is provisioned separately through `Bicep`.

## Transactional Outbox

Incident creation and retry operations must persist both:

```text
Incident state
+
AnalyseIncident outbox request
```

These are written atomically through `CosmosIncidentSubmissionStore`.

This avoids the dual-write failure case where an Incident could be saved successfully but the corresponding Service Bus message failed to publish.

The outbox document is later read through the Cosmos Change Feed by the Worker and published to Azure Service Bus.

## Service Bus

Infrastructure provides the implementation of: `IIncidentAnalysisQueue`

using `Azure Service Bus`.

The Infrastructure implementation serialises and sends an `AnalyseIncidentCommand` to: `analyse-incident`

The API does not publish directly to Service Bus. Instead:

```text
API/Application
→ persist Incident + Outbox atomically
→ Cosmos Change Feed
→ IncidentOutboxWorker
→ IIncidentAnalysisQueue
→ Azure Service Bus
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
