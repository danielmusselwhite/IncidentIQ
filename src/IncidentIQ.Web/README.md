# IncidentIQ.Web

`IncidentIQ.Web` is the React frontend for IncidentIQ.

It provides the UI engineers use to submit incidents, track processing status, inspect incident details, and manage operational Runbooks.

The frontend is built with React, TypeScript, Vite, and React Router.

It communicates only with `IncidentIQ.Api`; it does not connect directly to Cosmos DB, Service Bus, or other Azure services.

## Current Functionality

- Incident dashboard and search.
- Incident submission.
- Incident detail views.
- Automatic polling while incidents are `Queued` or `Processing`.
- Severity and status display.
- Runbook list, create, view, edit, and delete flows.
- Loading, empty, validation, not-found, and error states.
- Shared application layout/navigation.
- Typed API clients.

Planned UI work includes AI analysis results, similar incidents, supporting evidence, feedback, and Operations/Admin screens.

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
├── App.tsx
└── main.tsx
```

Pages own page-level state and reusable behaviour is extracted into components or API modules where appropriate.

## Frontend Flow

```text
React Page
   ↓
Typed API Client
   ↓
IncidentIQ.Api
```

Creating an incident:

```text
SubmitIncidentPage
      ↓
incidentsApi.ts
      ↓
POST /api/incidents
      ↓
API returns created Incident
      ↓
navigate to IncidentDetailPage
      ↓
poll GET /api/incidents/{id}
      ↓
Queued → Processing → Completed / Failed
```

The asynchronous outbox, Change Feed, Service Bus, and Worker pipeline remains a backend concern.

For the full backend flow, see the repository [README](../../README.md).

## API Layer

Backend calls are isolated under:

```text
src/api/
```

These modules handle:

- HTTP calls to the ASP.NET Core API.
- Request serialization.
- Typed response deserialization.
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

`AppLayout` provides the shared navigation around these pages.

## Local Development

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
