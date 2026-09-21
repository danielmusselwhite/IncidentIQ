# IncidentIQ.Web

React/TypeScript engineer interface.

## Features

- Microsoft Entra login/logout with MSAL.
- Incident dashboard, submission and grounded analysis.
- Runbook CRUD and semantic search.
- Operational Assistant with answer-scoped evidence.
- Shared layout, loading/error states and authenticated user profile.

## Authentication

```text
AuthenticationGate
→ MSAL session
→ acquireTokenSilent(access_as_user)
→ apiClient
→ Authorization: Bearer <token>
→ IncidentIQ API
```

Auth configuration:

```text
VITE_ENTRA_TENANT_ID
VITE_ENTRA_CLIENT_ID
VITE_API_SCOPE
VITE_API_BASE_URL
```

Machine-specific Entra values belong in `.env.local`.

All backend calls should go through `src/api/apiClient.ts`; feature API modules should not duplicate token handling.

## Routes

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

## Run

```powershell
npm install
npm run dev
npm run build
npm run lint
```
