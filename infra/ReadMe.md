# IncidentIQ Infrastructure

The `infra` folder contains the Azure Infrastructure as Code for IncidentIQ.

Azure resources are defined with Bicep and deployed through GitHub Actions using OIDC. For teardown/recreation and environment refresh commands, see [Azure Dev Lifecycle](../docs/INCIDENTIQ-AZURE-DEV-LIFECYCLE.md).

## Structure

```text
infra/
├── bootstrap/
├── environments/
├── local/
│   └── servicebus/
├── modules/
├── main.bicep
└── ReadMe.md
```

Bootstrap resources are kept separate from the disposable application environment. The GitHub deployment identity uses an OIDC federated credential rather than a stored client secret.

## Development environment

The Bicep environment includes:

```text
rg-incidentiq-dev
├── Azure Container Registry
├── Azure Container Apps Environment
│   ├── API Container App
│   └── Worker Container App
├── Azure Static Web Apps
├── Azure Cosmos DB
│   └── IncidentIQ
│       ├── Incidents
│       ├── Runbooks
│       ├── RunbookChunks
│       ├── HistoricalIncidentVectors
│       └── ChangeFeedLeases
├── Azure Service Bus
│   ├── analyse-incident
│   ├── index-runbook
│   └── index-historical-incident
├── Azure OpenAI
│   ├── incident-analysis
│   └── runbook-embedding
├── API Managed Identity
├── Worker Managed Identity
├── Application Insights
└── Log Analytics
```

## Cosmos DB

| Container | Partition key | Purpose |
| --- | --- | --- |
| `Incidents` | `/incidentId` | Incident state, analysis outbox, analysis/evidence documents |
| `Runbooks` | `/id` | Editable operational Runbooks |
| `RunbookChunks` | `/runbookId` | Derived Runbook chunks and embeddings |
| `HistoricalIncidentVectors` | `/incidentId` | Derived completed-Incident embeddings and metadata |
| `ChangeFeedLeases` | `/id` | Change Feed Processor ownership/checkpoints |

The shared Incident partition supports transactional operations such as:

```text
Create Incident
→ Incident + analysis outbox

Complete analysis
→ completed Incident + analysis/evidence documents
```

`RunbookChunks` and `HistoricalIncidentVectors` are vector-enabled derived stores. Source records remain in `Runbooks` and `Incidents`.

Vector ranking is used for retrieval only; it is not exposed as a calibrated model-confidence score.

## Service Bus

Current queues:

```text
analyse-incident
→ durable Incident analysis commands

index-runbook
→ durable Runbook indexing commands

index-historical-incident
→ durable historical Incident indexing commands
```

Queues use bounded redelivery, dead-lettering and duplicate-detection settings.

The Worker identity needs only the sender/receiver permissions required by its hosted relays/consumers. The API does not directly publish Incident analysis work because submission uses the Cosmos transactional outbox.

## Azure OpenAI

The development environment uses:

```text
incident-analysis
→ gpt-5-mini

runbook-embedding
→ text-embedding-3-small
→ 1536 dimensions
```

The same chat deployment supports structured Incident analysis and the Operational Assistant; each uses its own response schema/prompting path.

The embedding deployment supports:

- Runbook indexing,
- historical Incident indexing,
- Runbook search,
- grounded Incident retrieval,
- Operational Assistant retrieval.

Both API and Worker require Azure OpenAI access:

```text
API
├── query embeddings
└── Operational Assistant chat generation

Worker
├── Incident analysis chat generation
├── Runbook embeddings
└── historical Incident embeddings
```

Deployed workloads authenticate through managed identity.

## Container Apps

```text
API Container App
├── external HTTPS ingress
├── API managed identity
├── Cosmos access
├── ACR pull
└── Azure OpenAI access

Worker Container App
├── no public ingress
├── Worker managed identity
├── Cosmos access
├── ACR pull
├── Service Bus sender/receiver roles
└── Azure OpenAI access
```

KEDA-driven scaling is a later stage; the current Worker hosting keeps the asynchronous pipelines deliberately simple while the feature set is still evolving.

## Workload identities

### API identity

The API uses Managed Identity for Cosmos and Azure OpenAI. It does not need Service Bus access for normal Incident submission.

### Worker identity

The Worker hosts six main background services:

```text
IncidentOutboxWorker
AnalyseIncidentWorker
RunbookIndexChangeFeedWorker
IndexRunbookWorker
HistoricalIncidentIndexChangeFeedWorker
IndexHistoricalIncidentWorker
```

It needs:

- Cosmos data access,
- Service Bus sender/receiver permissions for the relevant queues,
- Azure OpenAI access,
- ACR pull.

A useful lesson from Stage 12 was that a relay which **sends** an indexing command needs `Azure Service Bus Data Sender`; receiver permission alone is not sufficient.

## Monitoring

Application Insights and Log Analytics provide the telemetry foundation.

Current AI adapters record structured metadata such as duration, deployment/model and failure category without logging raw Incident, Runbook, prompt or model-response payloads.

Full distributed tracing, dashboards/KQL and KEDA/queue telemetry are later-stage work.

## GitHub Actions

Deployment uses GitHub OIDC:

```text
Pull request → master
→ tests
→ Bicep validation
→ Azure What-If

Push → master / manual trigger
→ tests
→ provision/update infrastructure
→ build/push API + Worker images
→ deploy Container Apps
→ build/deploy React frontend
```

## Local infrastructure

Docker Compose provides local equivalents where practical:

```text
IncidentIQ.Api
IncidentIQ.Worker
IncidentIQ.Web
Cosmos DB Emulator
Service Bus Emulator
└── SQL Server dependency
```

Azure OpenAI is not emulated. `Development` selects deterministic Incident analysis, embedding and Operational Assistant implementations.

The Service Bus Emulator queue definition lives under:

```text
infra/local/servicebus/Config.json
```

For startup and Azure-connected debugging, see [Development](../docs/DEVELOPMENT.md).

## Resource ownership

| Resource | Defined in |
| --- | --- |
| Bootstrap identity/RBAC | `bootstrap/` |
| Cosmos DB, containers and Cosmos RBAC | `modules/cosmos.bicep` |
| Service Bus queues and messaging RBAC | `modules/service-bus.bicep` |
| Azure OpenAI deployments and AI RBAC | `modules/azure-ai.bicep` |
| ACR | `modules/acr.bicep` |
| Container Apps Environment | `modules/container-apps-environment.bicep` |
| API Container App / identity | API modules |
| Worker Container App / identity | Worker modules |
| Static Web App | `modules/frontend.bicep` |
| Application Insights / Log Analytics | monitoring modules |

## Infrastructure principles

- Define Azure resources in Bicep.
- Keep bootstrap resources separate from disposable application resources.
- Use OIDC for GitHub and Managed Identity for workloads.
- Apply least-privilege RBAC where practical.
- Keep source data separate from derived vector data.
- Keep application retry/business policy in application configuration rather than IaC.
- Prefer local emulators and deterministic AI for normal feature development.
