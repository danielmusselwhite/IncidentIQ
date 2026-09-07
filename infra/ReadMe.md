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

Current development resources include:

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
│       └── ChangeFeedLeases
├── Azure Service Bus
│   └── analyse-incident
│       └── $DeadLetterQueue
├── Azure OpenAI
│   └── incident-analysis deployment
├── API Managed Identity
├── Worker Managed Identity
├── Application Insights
└── Log Analytics
```

The repository root [README](../ReadMe.md) contains Mermaid diagrams for the deployed infrastructure, internal application architecture, and Incident submission/message flow.

## Cosmos DB

Defined in `modules/cosmos.bicep`.

| Container | Partition key | Purpose |
|---|---|---|
| `Incidents` | `/incidentId` | Incident, `AnalyseIncident` outbox, and structured analysis documents |
| `Runbooks` | `/id` | Editable operational Runbooks |
| `ChangeFeedLeases` | `/id` | SDK-managed Change Feed Processor checkpoints/ownership |

The shared `/incidentId` partition allows two important atomic operations:

```text
Create / Retry
→ Incident + analysis outbox

Complete Analysis
→ Completed Incident + IncidentAnalysisDocument
```

Both use Cosmos transactional batches inside one logical partition.

See [Design Decisions & Trade-offs](../docs/DESIGN-DECISIONS.md) for the reasoning behind the outbox and partition-key change.

## Service Bus

Defined in `modules/service-bus.bicep`.

`analyse-incident` carries durable analysis commands and is configured for bounded redelivery, dead-lettering, TTL, and duplicate detection. The queue's `maxDeliveryCount` and the Worker's `ServiceBus__MaxDeliveryCount` setting are sourced from the same infrastructure value so application and broker behaviour stay aligned.

The API does not require Service Bus sender access because it persists an outbox instead. The Worker needs sender access for the outbox relay and receiver access for analysis consumption.

## Azure AI

Defined in `modules/azure-ai.bicep`.

The development environment provisions an Azure OpenAI account and the `incident-analysis` model deployment. The Worker receives the Azure AI endpoint, deployment name, and model name through Container App configuration and authenticates with its managed identity.

The Worker identity is assigned `Cognitive Services OpenAI User` on the Azure OpenAI resource.

Application-level resilience settings such as bounded SDK retries and request/network timeouts live in the Worker/Infrastructure configuration; they do not require extra Azure resources.

## Container Hosting

The API and Worker run in a shared Azure Container Apps Environment connected to Log Analytics.

```text
API Container App
├── external HTTPS ingress
├── scale-to-zero enabled
├── API managed identity
└── Cosmos + ACR access

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

The API uses Managed Identity for Cosmos DB and ACR. With the transactional outbox architecture, it does not publish directly to Service Bus.

### Worker Identity

The Worker host runs both background services:

```text
IncidentOutboxWorker
→ Cosmos Change Feed
→ Service Bus Data Sender

AnalyseIncidentWorker
→ Service Bus Data Receiver
→ Azure OpenAI
→ Cosmos analysis persistence
```

It therefore requires Cosmos DB Data Contributor, queue-scoped Service Bus Data Sender/Data Receiver, and Cognitive Services OpenAI User access.

## Monitoring

`application-insights.bicep`, `log-analytics.bicep`, and `container-apps-environment.bicep` provide the telemetry foundation.

Stage 10 now emits structured AI success/failure logs from `AzureIncidentAnalyzer`, including analysis duration, failure category, deployment, and model. Full OpenTelemetry dependency tracing, dashboards, KQL, queue metrics, and scaling telemetry remain Stage 15 work.

## GitHub Actions

Deployment authentication uses GitHub OIDC and the `development` GitHub Environment.

```text
Pull request → main
→ tests
→ Bicep validation
→ Azure What-If

Push → main / manual trigger
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

Azure OpenAI itself is not emulated. Instead, when the Worker runs with `DOTNET_ENVIRONMENT=Development`, `DevelopmentDummyIncidentAnalyzer` provides deterministic structured analysis while the rest of the asynchronous pipeline uses the local emulators.

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
| Azure OpenAI / model deployment / Worker AI RBAC | `modules/azure-ai.bicep` |
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
