# IncidentIQ Infrastructure

Azure infrastructure is defined with Bicep and deployed through GitHub Actions using OIDC.

## Layout

```text
infra/
├── bootstrap/      # GitHub deployment identity/OIDC
├── environments/   # environment parameters
├── local/          # emulator configuration
├── modules/
└── main.bicep
```

## Development Environment

```text
rg-incidentiq-dev
├── Container Registry
├── Container Apps Environment
│   ├── API
│   └── Worker (KEDA 1–3 replicas)
├── Static Web Apps
├── Cosmos DB
├── Service Bus
├── Azure OpenAI
├── API Managed Identity
├── Worker Managed Identity
├── Application Insights
└── Log Analytics
```

### Cosmos

| Container | Partition key | Purpose |
| --- | --- | --- |
| `Incidents` | `/incidentId` | Incident, outbox, analysis/evidence |
| `Runbooks` | `/id` | Source Runbooks |
| `RunbookChunks` | `/runbookId` | Derived Runbook vectors |
| `HistoricalIncidentVectors` | `/incidentId` | Derived Incident vectors |
| `ChangeFeedLeases` | `/id` | Change Feed state |

### Service Bus

```text
analyse-incident
index-runbook
index-historical-incident
```

Queues use bounded delivery attempts, DLQs and duplicate detection.

The Worker uses queue-scoped sender/receiver RBAC. The `analyse-incident` queue also grants the Worker identity Data Owner access so KEDA can read queue runtime state.

### Azure OpenAI

```text
incident-analysis → gpt-5-mini
runbook-embedding → text-embedding-3-small (1536)
```

### Observability & Scaling

API and Worker export OpenTelemetry to Application Insights/Log Analytics.

Worker KEDA configuration:

```text
trigger:          analyse-incident Service Bus queue
min replicas:     1
max replicas:     3
target:           2 queued messages / replica
polling interval: 15 seconds
```

Minimum 1 is intentional because the Worker also owns Cosmos Change Feed relays.

## Identity

- GitHub deploys with OIDC.
- API/Worker use Managed Identity for Azure services.
- API user authentication uses separate Microsoft Entra app registrations; user access tokens are not workload credentials.

## RBAC

- API: Cosmos, Azure OpenAI, ACR pull.
- Worker: Cosmos, Service Bus send/receive, queue-scoped scaler access, Azure OpenAI, ACR pull.

## Deployment

```text
tests
→ Bicep validation/What-If
→ infrastructure
→ build/push API + Worker
→ deploy Container Apps
→ build/deploy React
```

Lifecycle commands: [Azure Dev Lifecycle](../docs/INCIDENTIQ-AZURE-DEV-LIFECYCLE.md).
