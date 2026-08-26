# IncidentIQ.Worker

`IncidentIQ.Worker` is the .NET background processing host for IncidentIQ.

It consumes asynchronous incident-analysis commands from Azure Service Bus and delegates the actual workflow to the Application layer.

The Worker exists so long-running analysis work does not need to happen inside the HTTP request that creates an incident.

---

## Current Flow

```text
Service Bus
    ↓
analyse-incident
    ↓
AnalyseIncidentWorker
    ↓
AnalyseIncidentHandler
    ↓
Cosmos DB
```

When an incident is submitted:

```text
Incident created
      ↓
Status: Queued
      ↓
AnalyseIncident command
      ↓
Service Bus
      ↓
Worker receives message
      ↓
Status: Processing
      ↓
temporary analysis workflow
      ↓
Status: Completed
```

The actual Azure AI / RAG processing will replace the temporary analysis step later.

---

## Responsibilities

Current responsibilities include:

- Connect to the `analyse-incident` Service Bus queue.
- Receive `AnalyseIncidentCommand` messages.
- Deserialize messages.
- Add correlation, incident, and command IDs to logging scope.
- Dead-letter permanently invalid messages.
- Invoke `AnalyseIncidentHandler`.
- Explicitly complete successful messages.
- Leave failed processing available for Service Bus retry behaviour.

---

## Structure

```text
IncidentIQ.Worker/
├── AnalyseIncidentWorker.cs
├── Program.cs
├── Dockerfile
├── appsettings.json
└── Properties/
```

### `AnalyseIncidentWorker`

Handles Service Bus transport concerns.

It should remain focused on:

```text
receive
→ validate message
→ invoke Application
→ settle message
```

Business workflow logic belongs in the Application layer.

### `Program.cs`

Configures:

- Application dependencies.
- Infrastructure dependencies.
- Hosted Worker service.

---

## Local Development

The Worker can run as part of Docker Compose with:

```text
Cosmos DB Emulator
Service Bus Emulator
```

or directly with:

```powershell
dotnet run --project src\IncidentIQ.Worker
```

when configured to use Azure development resources.

---

## Planned Responsibilities

Later stages will add:

- Retry/failure metadata.
- Idempotent processing.
- Final Failed handling.
- AI analysis.
- Embeddings and vector retrieval.
- Runbook indexing.
- Completion events.
- Worker telemetry.
- KEDA-based scaling when deployed to Azure Container Apps.
