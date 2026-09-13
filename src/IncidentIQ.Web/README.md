# IncidentIQ.Web

`IncidentIQ.Web` is the React/TypeScript engineer interface for IncidentIQ.

It talks only to the ASP.NET Core API; Cosmos DB, Service Bus and Azure OpenAI remain backend concerns.

## Current experiences

- Incident dashboard and submission.
- Incident detail/status polling.
- Grounded analysis result with historical Incident and Runbook evidence.
- Runbook list/create/view/edit/delete.
- Semantic Runbook search.
- Dedicated `/assistant` Operational Assistant.
- Service/environment filters for Assistant retrieval.
- Answer-scoped `HI-*` / `RB-*` citations and evidence inspector.
- Loading, empty, validation, not-found and API error states.

## Operational Assistant

Conversation state is intentionally kept in React for the current browser session.

```text
current question
+ recent user/Assistant turns
+ optional filters
      ↓
POST /api/assistant/questions
      ↓
answer sections + exact evidence
      ↓
chat + evidence inspector
```

The backend remains stateless. Recent conversation turns are resent with each request so follow-up questions make sense.

Each Assistant message keeps its own evidence payload because `HI-1` or `RB-1` is only meaningful within the response that created it.

Conversation persistence is deferred until authenticated user identity exists.

## API layer

Backend calls are isolated under `src/api/`.

Typical modules include:

```text
incidentsApi.ts
runbooksApi.ts
assistantApi.ts
apiError.ts
```

Problem Details responses are converted to the shared `ApiError` type.

The backend URL is configured with:

```text
VITE_API_BASE_URL
```

## Main routes

```text
/incidents
/incidents/new
/incidents/:id

/runbooks
/runbooks/new
/runbooks/:id
/runbooks/:id/edit

/assistant
```

`AppLayout` provides the shared sidebar/top-level navigation.

## Local development

```powershell
npm install
npm run dev
```

Typical URL:

```text
http://localhost:5173
```

Build:

```powershell
npm run build
```

The frontend does not need to know whether the backend is using deterministic Development AI or real Azure OpenAI.
