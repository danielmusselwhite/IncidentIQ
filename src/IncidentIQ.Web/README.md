# IncidentIQ.Web

React/TypeScript engineer interface.

## Features

- Microsoft Entra login/logout with MSAL.
- Engineer/Administrator role-aware UI.
- Incident dashboard, submission and grounded analysis.
- Runbook CRUD and semantic search.
- Operational Assistant with answer-scoped evidence.
- Administrator Operations page with status summary, failed work and retry controls.
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

Configuration:

```text
VITE_ENTRA_TENANT_ID
VITE_ENTRA_CLIENT_ID
VITE_API_SCOPE
VITE_API_BASE_URL
```

Machine-specific Entra values belong in `.env.local`.

All backend calls go through `src/api/apiClient.ts`; feature API modules do not duplicate token handling. The client restores an account from MSAL's cached accounts when no active account has yet been selected, avoiding first-load failures after redirect login.

The UI hides Administrator-only navigation/actions for Engineers, but API authorization remains the security boundary.

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
/operations          # Administrator UI
```

## Run

```powershell
npm install
npm run dev
npm run build
npm run lint
```
