# IncidentIQ.Web

`IncidentIQ.Web` is the React frontend for IncidentIQ.

It provides the UI engineers use to submit incidents, track asynchronous processing, inspect persisted AI analysis, and manage operational Runbooks.

The frontend is built with React, TypeScript, Vite, and React Router. It communicates only with `IncidentIQ.Api`; it does not connect directly to Cosmos DB, Service Bus, or Azure OpenAI.

## Current Functionality

- Incident dashboard and search.
- Incident submission.
- Incident detail views.
- Automatic polling while incidents are `Queued` or `Processing`.
- Severity and status display.
- Persisted AI-analysis retrieval after an Incident reaches `Completed`.
- AI summary display.
- Likely causes with confidence scores.
- Recommended actions.
- Analysis model/time metadata.
- Runbook list, create, view, edit, and delete flows.
- Loading, empty, validation, not-found, analysis-failure, and general error states.
- Shared application layout/navigation.
- Typed API clients and TypeScript models.

Future UI work includes similar historical Incidents, supporting Runbook evidence/citations, feedback, and Operations/Admin screens.

## Structure

```text
src/
├── api/
│   ├── incidentsApi.ts
│   ├── runbooksApi.ts
│   └── apiError.ts
├── components/
├── layout/
├── pages/
│   ├── Incidents/
│   ├── IncidentDetail/
│   ├── SubmitIncident/
│   ├── Runbooks/
│   ├── RunbookDetail/
│   ├── CreateRunbook/
│   └── EditRunbook/
├── types/
│   ├── incident.ts
│   └── incidentAnalysis.ts
├── App.tsx
└── main.tsx
```

## Incident Detail Flow

```text
SubmitIncidentPage
      ↓
POST /api/incidents
      ↓
navigate to /incidents/{id}
      ↓
IncidentDetailPage
      ↓
poll GET /api/incidents/{id}
      ↓
Queued → Processing → Completed / Failed
      ↓
if Completed
      ↓
GET /api/incidents/{id}/analysis
      ↓
render summary / causes / actions
```

Polling occurs on an interval while processing is active and stops when a terminal state is reached or the page unmounts.

The asynchronous outbox, Change Feed, Service Bus, Worker, and Azure AI pipeline remains a backend concern.

For the complete message flow, see the repository [README](../../ReadMe.md).

## API Layer

Backend calls are isolated under:

```text
src/api/
```

These modules handle:

- HTTP calls to the ASP.NET Core API.
- Request serialization.
- Typed response deserialization.
- Persisted analysis retrieval.
- Conversion of Problem Details responses into `ApiError`.

The API base URL is configured with:

```text
VITE_API_BASE_URL
```

## Routing

Current routes include:

```text
/incidents
/incidents/new
/incidents/:id
/runbooks
/runbooks/new
/runbooks/:id
/runbooks/:id/edit
```

`AppLayout` provides shared navigation around these pages.

## Local Development

When Docker Compose is used, the complete backend flow uses emulators plus deterministic local AI. The frontend does not need to know which `IIncidentAnalyzer` implementation is active.

To run Vite directly:

```powershell
npm install
npm run dev
```

The frontend normally runs at:

```text
http://localhost:5173
```

## Build

```powershell
npm run build
```

## Design Approach

The frontend intentionally remains straightforward: page state stays close to pages, API calls stay outside components, shared models remain typed, and backend infrastructure remains hidden behind the API.
