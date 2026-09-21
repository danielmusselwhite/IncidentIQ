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
│   └── Worker
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

Queues use retries, DLQs and duplicate detection.

### Azure OpenAI

```text
incident-analysis → gpt-5-mini
runbook-embedding → text-embedding-3-small (1536)
```

## Identity

- GitHub deploys with OIDC.
- API/Worker use Managed Identity for Azure services.
- API user authentication uses separate Microsoft Entra app registrations; user access tokens are not workload credentials.

## RBAC

- API: Cosmos, Azure OpenAI, ACR pull.
- Worker: Cosmos, Service Bus send/receive, Azure OpenAI, ACR pull.

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
