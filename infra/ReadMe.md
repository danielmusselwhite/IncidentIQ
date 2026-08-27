# IncidentIQ Infrastructure

The `infra` folder contains the Infrastructure as Code for IncidentIQ.

Azure resources are defined with Bicep and deployed through GitHub Actions using OIDC authentication.

For environment teardown/recreation and configuration refresh commands, see [IncidentIQ Azure Dev Environment Lifecycle](../docs/INCIDENTIQ-AZURE-DEV-LIFECYCLE.md).

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
│   ├── api-identity.bicep
│   ├── application-insights.bicep
│   ├── cosmos.bicep
│   ├── log-analytics.bicep
│   ├── service-bus.bicep
│   └── worker-identity.bicep
├── main.bicep
└── README.md
```

## Bootstrap Infrastructure

Bootstrap infrastructure is deliberately separate from the disposable development environment.

```text
rg-incidentiq-bootstrap
└── GitHub deployment managed identity
    └── OIDC federated credential
```

The deployment identity receives resource-group-scoped permissions on:

```text
rg-incidentiq-dev
├── Contributor
└── Role Based Access Control Administrator
```

This allows GitHub Actions to recreate the development environment and its workload RBAC assignments without storing an Azure client secret.

## Environment Infrastructure

`main.bicep` is the composition root for the application environment and receives environment-specific values from:

```text
environments/dev.bicepparam
```

Current development resources include:

```text
rg-incidentiq-dev
├── Azure Cosmos DB
│   └── IncidentIQ
│       ├── Incidents
│       ├── Runbooks
│       └── ChangeFeedLeases
├── Azure Service Bus
│   └── analyse-incident
│       └── $DeadLetterQueue
├── API Managed Identity
├── Worker Managed Identity
├── Application Insights
└── Log Analytics
```

## Cosmos DB

Defined in:

```text
modules/cosmos.bicep
```

Current containers:

| Container | Partition key | Purpose |
|---|---|---|
| `Incidents` | `/incidentId` | Incident documents and `AnalyseIncident` outbox documents |
| `Runbooks` | `/id` | Editable operational Runbooks |
| `ChangeFeedLeases` | `/id` | SDK-managed Change Feed Processor checkpoints/ownership |

The shared `/incidentId` partition allows the API to atomically create:

```text
IncidentDocument
+
IncidentAnalysisOutboxDocument
```

using a single Cosmos transactional batch.

The Change Feed Processor uses `ChangeFeedLeases` to relay outbox entries asynchronously.

See [Design Decisions & Trade-offs](../docs/DESIGN-DECISIONS.md) for the reasoning behind the outbox and partition-key change.

## Service Bus

Defined in:

```text
modules/service-bus.bicep
```

Current queue:

```text
analyse-incident
└── $DeadLetterQueue
```

The queue is configured for bounded redelivery, dead-lettering, TTL, and duplicate detection.

`AnalyseIncident` is a command and is therefore carried through a queue rather than an event topic.

## Workload Identities

### API Identity

The API requires Cosmos access for incident and Runbook persistence.

With the transactional outbox, the API no longer needs to publish the create-incident analysis command directly to Service Bus.

### Worker Identity

The Worker host runs both the outbox relay and analysis consumer.

When deployed to Azure it therefore requires permissions for:

```text
Cosmos DB
└── read Change Feed / leases and read-update Incidents

Service Bus
├── Data Sender   (IncidentOutboxWorker)
└── Data Receiver (AnalyseIncidentWorker)
```

Before the Worker Container App is deployed, Bicep RBAC should be aligned with these responsibilities. Any legacy API Service Bus sender assignment can be removed once the outbox architecture is fully reflected in Azure RBAC.

## Monitoring

`application-insights.bicep` and `log-analytics.bicep` create the initial telemetry resources.

The API already supports Application Insights / Azure Monitor integration. Worker and distributed telemetry will be expanded during the observability stage.

## GitHub Actions Deployment

Infrastructure deployments use GitHub OIDC rather than a client secret.

Typical flow:

```text
GitHub Actions
      ↓ OIDC
Deployment Identity
      ↓
Bicep validation / what-if
      ↓
rg-incidentiq-dev
```

Normal environment deployments should be performed through the repository deployment workflow.

## Local Infrastructure

Docker Compose provides local equivalents where practical:

```text
IncidentIQ.Api
IncidentIQ.Worker
Cosmos DB Emulator
Service Bus Emulator
└── SQL Server dependency
```

The Service Bus Emulator queue definition is stored at:

```text
infra/local/servicebus/Config.json
```

For startup commands and local URLs, see the [Development Guide](../docs/DEVELOPMENT.md).

## Resource Ownership

| Resource | Defined in |
|---|---|
| Bootstrap resource groups / deployment foundation | `bootstrap/main.bicep` |
| GitHub deployment identity | `bootstrap/github-identity.bicep` |
| Deployment RBAC | `bootstrap/deployment-role.bicep` |
| Cosmos DB / containers / Cosmos RBAC | `modules/cosmos.bicep` |
| Service Bus / queue / messaging RBAC | `modules/service-bus.bicep` |
| API identity | `modules/api-identity.bicep` |
| Worker identity | `modules/worker-identity.bicep` |
| Application Insights | `modules/application-insights.bicep` |
| Log Analytics | `modules/log-analytics.bicep` |

## Infrastructure Principles

- Define Azure resources in Bicep.
- Keep resource-specific configuration in modules.
- Keep environment values in `.bicepparam` files.
- Use OIDC rather than GitHub client secrets.
- Scope deployment permissions to the development resource group.
- Use Managed Identity and least-privilege RBAC where practical.
- Keep bootstrap resources separate from disposable application resources.
- Use local emulators for normal development where practical.
