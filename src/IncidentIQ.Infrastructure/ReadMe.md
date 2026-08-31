# IncidentIQ.Infrastructure

`IncidentIQ.Infrastructure` contains the concrete implementations used by the Application layer to communicate with external systems.

It is responsible for persistence, messaging, Azure SDK integration, and other technical concerns that should not live inside the Domain or Application projects.

---

## Responsibilities

Current responsibilities include:

- Cosmos DB client configuration.
- Incident persistence.
- Atomic Incident + analysis outbox persistence.
- Atomic completed Incident + structured analysis persistence.
- Runbook persistence.
- Local Cosmos initialization.
- Azure Service Bus client configuration.
- Sending `AnalyseIncidentCommand` messages.
- Azure OpenAI incident analysis using structured outputs.
- Azure authentication through `DefaultAzureCredential` where configured.
- Dependency injection registration for Infrastructure services.

---

## High-Level Structure

```text
IncidentIQ.Infrastructure/

├── AzureAI/
│   ├── AzureAIOptions.cs
│   ├── AzureIncidentAnalyzer.cs
│   ├── AzureIncidentAnalysisResponse.cs
│   └── AzureIncidentAnalysisSchema.cs
│
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
│       ├── CosmosIncidentAnalysisStore.cs
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

The `Incidents` container stores Incident, analysis-outbox, and structured analysis documents.

These document types share the same `incidentId`. This allows Incident submission/retry + outbox persistence, and completed Incident + analysis persistence, to use Cosmos transactional batches within one logical partition.

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

## Azure AI

`AzureIncidentAnalyzer` implements `IIncidentAnalyzer` using Azure OpenAI. It sends the Incident as chat input, requires a strict structured-output schema, validates the returned data, and maps it to `IncidentAnalysisResult`.

Azure AI dependencies are registered separately through `AddAzureAIDependencies`, so only the Worker requires Azure AI configuration.

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
