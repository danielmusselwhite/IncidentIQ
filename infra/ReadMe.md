# IncidentIQ Infrastructure

The `infra` folder contains the Azure Infrastructure as Code for IncidentIQ.

Azure resources are defined with Bicep and deployed through GitHub Actions using OIDC authentication. For environment teardown/recreation and configuration refresh commands, see [IncidentIQ Azure Dev Environment Lifecycle](../docs/INCIDENTIQ-AZURE-DEV-LIFECYCLE.md).

## Structure

```text
infra/
├── bootstrap/
│   ├── main.bicep
│   ├── github-identity.bicep
│   └── deployment-role.bicep
├── environments/
│   └── dev.bicepparam
├── local/
│   └── servicebus/
│       └── Config.json
├── modules/
│   ├── acr.bicep
│   ├── api-container-app.bicep
│   ├── api-identity.bicep
│   ├── application-insights.bicep
│   ├── azure-ai.bicep
│   ├── container-apps-environment.bicep
│   ├── cosmos.bicep
│   ├── frontend.bicep
│   ├── log-analytics.bicep
│   ├── service-bus.bicep
│   ├── worker-container-app.bicep
│   └── worker-identity.bicep
├── main.bicep
└── ReadMe.md
```

## Bootstrap Infrastructure

Bootstrap infrastructure is deliberately separate from the disposable development environment.

```text
rg-incidentiq-bootstrap
└── GitHub deployment managed identity
    └── OIDC federated credential
```

The deployment identity receives resource-group-scoped permissions on `rg-incidentiq-dev` including Contributor, Role Based Access Control Administrator, and ACR push access.

This lets GitHub Actions provision the development environment, create workload RBAC assignments, and push application images without storing an Azure client secret.

## Development Environment

`main.bicep` is the composition root for the application environment and receives environment-specific values from `environments/dev.bicepparam`.

The Bicep development-environment definition currently includes:

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
│       └── ChangeFeedLeases
├── Azure Service Bus
│   ├── analyse-incident
│   │   └── $DeadLetterQueue
│   └── index-runbook
│       └── $DeadLetterQueue
├── Azure OpenAI
│   ├── incident-analysis deployment
│   └── runbook-embedding deployment
├── API Managed Identity
├── Worker Managed Identity
├── Application Insights
└── Log Analytics
```

Bicep defines the `RunbookChunks` vector container, `index-runbook` queue, Runbook embedding deployment, and the API/Worker permissions and configuration required to use that embedding deployment.

The repository root [README](../ReadMe.md) contains Mermaid diagrams for the infrastructure, internal application architecture, and asynchronous message flows.

## Cosmos DB

Defined in `modules/cosmos.bicep`.

| Container | Partition key | Purpose |
|---|---|---|
| `Incidents` | `/incidentId` | Incident, `AnalyseIncident` outbox, and structured analysis documents |
| `Runbooks` | `/id` | Editable operational Runbooks and Change Feed source for indexing |
| `RunbookChunks` | `/runbookId` | Derived Runbook chunks, retrieval metadata, and 1536-dimension embeddings |
| `ChangeFeedLeases` | `/id` | SDK-managed Change Feed Processor checkpoints/ownership |

The shared `/incidentId` partition allows two important atomic operations:

```text
Create / Retry
→ Incident + analysis outbox

Complete Analysis
→ Completed Incident + IncidentAnalysisDocument
```

Both use Cosmos transactional batches inside one logical partition.

`RunbookChunks` is configured with a `/embedding` `float32` vector policy using cosine distance and a `quantizedFlat` index. The embedding path is excluded from the ordinary Cosmos index. Grouping chunks by `/runbookId` lets re-indexing replace stale chunks inside one logical partition.

Stage 11B queries this container through `VectorDistance`, returning top-K chunk matches with optional service metadata filtering. The Infrastructure retriever records query latency and Cosmos request-unit (RU) consumption; lower returned distance values are stronger semantic matches.

See [Design Decisions & Trade-offs](../docs/DESIGN-DECISIONS.md) for the reasoning behind the outbox and partition-key change.

## Service Bus

Defined in `modules/service-bus.bicep`.

Two queues are currently provisioned:

```text
analyse-incident
→ durable Incident analysis commands

index-runbook
→ durable Runbook indexing commands
```

Both use bounded redelivery, dead-lettering, TTL, and duplicate detection. The Worker's queue-scoped RBAC grants sender/receiver access only where its hosted services require it. The API does not require Service Bus access: Incident submission uses the Cosmos outbox, while Runbook indexing is initiated by the Worker-side Runbooks Change Feed relay.

## Azure AI

Defined in `modules/azure-ai.bicep`.

The development environment provisions one Azure OpenAI account with two deployments:

```text
incident-analysis → gpt-5-mini
runbook-embedding → text-embedding-3-small (1536 dimensions)
```

Both backend hosts receive the Azure AI configuration they need through Container App environment variables and authenticate with managed identity:

```text
API
└── runbook-embedding configuration for semantic Runbook search

Worker
├── incident-analysis configuration
└── runbook-embedding configuration for Runbook ingestion
```

Both the API and Worker identities are assigned `Cognitive Services OpenAI User` on the Azure OpenAI resource. The API needs this role for query embeddings; the Worker needs it for Incident analysis and Runbook ingestion embeddings.

Application-level resilience settings such as bounded SDK retries and request/network timeouts live in the Worker/Infrastructure configuration; they do not require extra Azure resources.

## Container Hosting

The API and Worker run in a shared Azure Container Apps Environment connected to Log Analytics.

```text
API Container App
├── external HTTPS ingress
├── scale-to-zero enabled
├── API managed identity
├── Cosmos + ACR access
└── Azure OpenAI embedding access

Worker Container App
├── no ingress
├── one replica kept running before KEDA stage
├── Worker managed identity
├── Cosmos + ACR access
├── Service Bus sender + receiver access
└── Azure OpenAI access
```

The Worker remains at one replica until queue/KEDA scaling is introduced later.

## Container Registry

ACR stores the API and Worker images.

- Admin credentials are disabled.
- Anonymous pull is disabled.
- API and Worker managed identities receive `AcrPull`.
- The GitHub deployment identity receives push access through bootstrap RBAC.
- Container images are tagged as `<VersionPrefix>-<short-git-sha>` for traceability.

Example:

```text
incidentiq-api:1.0.0-a83bf21
incidentiq-worker:1.0.0-a83bf21
```

## Frontend Hosting

The React/Vite frontend is hosted in Azure Static Web Apps. Bicep provisions the Static Web App resource; GitHub Actions builds the frontend with the deployed API URL and uploads the generated `dist` directory.

## Workload Identities

### API Identity

The API uses Managed Identity for Cosmos DB, ACR, and Azure OpenAI. With the transactional outbox architecture, it does not publish directly to Service Bus. Stage 11B adds Azure OpenAI access because semantic Runbook search generates its query embedding synchronously in the API path.

### Worker Identity

The Worker host currently runs four background services:

```text
IncidentOutboxWorker
→ Incidents Change Feed → analyse-incident

AnalyseIncidentWorker
→ analyse-incident → Azure OpenAI → Cosmos analysis persistence

RunbookIndexChangeFeedWorker
→ Runbooks Change Feed → index-runbook

IndexRunbookWorker
→ index-runbook → chunking/embeddings → RunbookChunks
```

It therefore requires Cosmos DB Data Contributor, queue-scoped Service Bus Data Sender/Data Receiver, and Cognitive Services OpenAI User access.

## Monitoring

`application-insights.bicep`, `log-analytics.bicep`, and `container-apps-environment.bicep` provide the telemetry foundation.

`AzureIncidentAnalyzer` emits structured AI success/failure logs including analysis duration, failure category, deployment, and model. Full OpenTelemetry dependency tracing, dashboards, KQL, queue metrics, and scaling telemetry remain future work.

## GitHub Actions

Deployment authentication uses GitHub OIDC and the `development` GitHub Environment.

```text
Pull request → master
→ tests
→ Bicep validation
→ Azure What-If

Push → master / manual trigger
→ tests
→ Bicep validation + What-If
→ provision/update Azure infrastructure
→ build + push API/Worker images to ACR
→ deploy Container App revisions
→ build React with the deployed API URL
→ deploy frontend to Static Web Apps
```

Normal environment deployments should be performed through repository workflows. Bootstrap infrastructure remains a separate, intentionally infrequent manual operation.

## Local Infrastructure

Docker Compose provides local equivalents where practical:

```text
IncidentIQ.Api
IncidentIQ.Worker
IncidentIQ.Web
Cosmos DB Emulator
Service Bus Emulator
└── SQL Server dependency
```

Azure OpenAI itself is not emulated. When the Worker runs with `DOTNET_ENVIRONMENT=Development`, `DevelopmentDummyIncidentAnalyzer` provides deterministic structured analysis and `DevelopmentDummyEmbeddingGenerator` provides deterministic 1536-dimensional vectors while Cosmos/Service Bus use the local emulators.

The Service Bus Emulator queue definition is stored at `infra/local/servicebus/Config.json`.

For startup commands and local URLs, see the [Development Guide](../docs/DEVELOPMENT.md).

## Resource Ownership

| Resource | Defined in |
|---|---|
| Bootstrap resource groups / deployment foundation | `bootstrap/main.bicep` |
| GitHub deployment identity | `bootstrap/github-identity.bicep` |
| Deployment RBAC | `bootstrap/deployment-role.bicep` |
| Cosmos DB / containers / Cosmos RBAC | `modules/cosmos.bicep` |
| Service Bus / queue / messaging RBAC | `modules/service-bus.bicep` |
| Azure OpenAI / model deployments / API + Worker AI RBAC | `modules/azure-ai.bicep` |
| Azure Container Registry / workload pull RBAC | `modules/acr.bicep` |
| Container Apps Environment | `modules/container-apps-environment.bicep` |
| API Container App | `modules/api-container-app.bicep` |
| Worker Container App | `modules/worker-container-app.bicep` |
| Static Web App | `modules/frontend.bicep` |
| API identity | `modules/api-identity.bicep` |
| Worker identity | `modules/worker-identity.bicep` |
| Application Insights | `modules/application-insights.bicep` |
| Log Analytics | `modules/log-analytics.bicep` |

## Infrastructure Principles

- Define Azure resources in Bicep.
- Keep resource-specific configuration in modules.
- Keep environment values in `.bicepparam` files.
- Use OIDC rather than GitHub client secrets.
- Use Managed Identity and least-privilege RBAC where practical.
- Keep bootstrap resources separate from disposable application resources.
- Keep application-level resilience policy in application configuration rather than encoding it as unrelated infrastructure.
- Tag deployed container images for source traceability.
- Use local emulators and deterministic local AI for normal development where practical.
