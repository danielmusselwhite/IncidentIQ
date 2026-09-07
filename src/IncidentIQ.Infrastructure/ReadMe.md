# IncidentIQ.Infrastructure

`IncidentIQ.Infrastructure` contains the concrete implementations used by the Application layer to communicate with external systems.

It is responsible for persistence, messaging, Azure/OpenAI SDK integration, deterministic local AI, and technical concerns that should not live inside Domain or Application.

## Responsibilities

Current responsibilities include:

- Cosmos DB client configuration and local initialization.
- Incident persistence.
- Atomic Incident + analysis-outbox persistence.
- Atomic completed Incident + structured-analysis persistence.
- Persisted analysis point reads.
- Runbook persistence.
- Azure Service Bus client configuration and `AnalyseIncidentCommand` publishing.
- Azure OpenAI structured incident analysis.
- Deterministic development incident analysis.
- Azure AI timeout/retry/failure classification and structured telemetry.
- Azure authentication through `DefaultAzureCredential` where configured.
- Infrastructure dependency-injection registration.

## High-Level Structure

```text
IncidentIQ.Infrastructure/
├── AzureAI/
│   ├── AzureAIOptions.cs
│   ├── AzureIncidentAnalyzer.cs
│   ├── DevelopmentDummyIncidentAnalyzer.cs
│   ├── AzureIncidentAnalysisResponse.cs
│   ├── AzureIncidentAnalysisSchema.cs
│   ├── AzureAIAnalysisException.cs
│   └── AzureAIFailureCategory.cs
├── Messaging/
│   ├── AzureServiceBusIncidentAnalysisQueue.cs
│   └── ServiceBusOptions.cs
├── Persistence/
│   └── Cosmos/
│       ├── CosmosOptions.cs
│       ├── CosmosInitializer.cs
│       ├── CosmosIncidentRepository.cs
│       ├── CosmosIncidentSubmissionStore.cs
│       ├── CosmosIncidentAnalysisStore.cs
│       ├── CosmosIncidentAnalysisReader.cs
│       ├── CosmosRunbookRepository.cs
│       └── Documents/
└── DependencyInjection.cs
```

## Cosmos DB

IncidentIQ uses the native Azure Cosmos DB SDK.

```text
IncidentIQ Database
├── Incidents          /incidentId
├── Runbooks           /id
└── ChangeFeedLeases   /id
```

The `Incidents` container stores multiple document types sharing the Incident partition:

```text
IncidentDocument
IncidentAnalysisOutboxDocument
IncidentAnalysisDocument
```

This supports two transactional batches:

```text
submission/retry
→ Incident + Outbox

successful analysis
→ Completed Incident + Analysis
```

`CosmosIncidentAnalysisReader` reads a persisted analysis using its deterministic ID (`analysis-{incidentId}`) and the raw Incident ID as partition key, giving an efficient point read.

## Transactional Outbox

Incident creation and deliberate retry operations persist both Incident state and an `AnalyseIncident` outbox request through `CosmosIncidentSubmissionStore`.

```text
API/Application
→ persist Incident + Outbox atomically
→ Cosmos Change Feed
→ IncidentOutboxWorker
→ IIncidentAnalysisQueue
→ Service Bus
```

This avoids the Cosmos + Service Bus dual-write failure mode.

## Service Bus

`AzureServiceBusIncidentAnalysisQueue` implements `IIncidentAnalysisQueue` and publishes `AnalyseIncidentCommand` to `analyse-incident`.

The API does not publish directly to Service Bus; only the Worker-side outbox relay uses the queue abstraction.

## Azure AI

### Development

```text
IIncidentAnalyzer
└── DevelopmentDummyIncidentAnalyzer
```

`AddDevelopmentAIDependencies()` is used when the Worker environment is `Development`. It returns deterministic structured analysis so the complete local workflow can be tested without Azure credentials or model cost.

### Azure / non-Development

```text
IIncidentAnalyzer
└── AzureIncidentAnalyzer
    ↓
Azure OpenAI ChatClient
```

`AzureIncidentAnalyzer`:

- Builds system/user chat messages from `IncidentAnalysisInput`.
- Requires the strict structured response schema.
- Deserializes and semantically validates the returned JSON.
- Maps Infrastructure response types to `IncidentAnalysisResult`.
- Uses an overall request timeout.
- Classifies throttling, service/client failures, timeout, and invalid model responses.
- Preserves caller cancellation semantics.
- Logs structured duration/success/failure metadata without logging the Incident payload or raw model response.

The Azure OpenAI client is long-lived and uses a bounded SDK retry policy plus an individual network timeout. Service Bus remains the durable outer retry mechanism.

Current defaults are:

```text
MaxRetries = 2
NetworkTimeoutSeconds = 60
RequestTimeoutSeconds = 90
```

## Authentication

Infrastructure supports two common modes:

```text
Docker Compose Development
→ emulator credentials / connection strings
→ DevelopmentDummyIncidentAnalyzer
```

```text
Azure / non-Development
→ DefaultAzureCredential
→ Managed Identity or developer Azure identity
→ Azure OpenAI / Cosmos / Service Bus RBAC
```

## Design Approach

- Application defines abstractions; Infrastructure implements them.
- Azure SDK types remain outside Domain/Application where practical.
- External-service configuration is bound through options classes.
- Long-lived SDK clients are registered and reused through dependency injection.
- Incident persistence and analysis persistence/read responsibilities remain separate.
- Resilience logic classifies and propagates failures rather than swallowing them, preserving Worker/Service Bus retry semantics.
