# IncidentIQ.Web

`IncidentIQ.Web` is the React frontend for IncidentIQ.

It provides the user interface engineers use to submit incidents, review incident status and details, and manage operational runbooks.

The frontend is built with:

- **React**
- **TypeScript**
- **Vite**
- **React Router**
- CSS modules/stylesheets organised alongside pages and components

The web application communicates only with the IncidentIQ ASP.NET Core API. It does not connect directly to Cosmos DB, Service Bus, or other Azure services.

---

## What the Frontend Does

Current functionality includes:

- Incident dashboard and search.
- Incident submission.
- Incident detail views.
- Incident severity and status display.
- Runbook list and management screens.
- Create, view, edit, and delete Runbooks.
- Loading, empty, validation, not-found, and error states.
- Shared application navigation and layout.
- Typed API clients for communication with the backend.

Planned frontend functionality includes:

- Automatic incident processing-status polling.
- AI analysis results.
- Similar incident and supporting evidence views.
- Analysis feedback.
- Operations and administration screens.

---

## High-Level Structure

The frontend follows a simple feature-oriented structure:

```text
src/
├── api/
│   ├── incidentsApi.ts
│   ├── runbooksApi.ts
│   └── apiError.ts
│
├── components/
│   └── RunbookForm/
│
├── layout/
│   └── AppLayout.tsx
│
├── pages/
│   ├── Incidents/
│   ├── IncidentDetail/
│   ├── SubmitIncident/
│   ├── Runbooks/
│   ├── RunbookDetail/
│   ├── CreateRunbook/
│   └── EditRunbook/
│
├── types/
│   ├── incident.ts
│   └── runbook.ts
│
├── App.tsx
└── main.tsx
```

Each page generally keeps its own React component and stylesheet together.

---

## Application Flow

At a high level:

```text
User
 ↓
React Page
 ↓
Typed API Client
 ↓
IncidentIQ.Api
 ↓
Backend application workflow
```

For example, creating an incident currently works like this:

```text
Submit Incident Page
        ↓
incidentsApi.ts
        ↓
POST /api/incidents
        ↓
IncidentIQ.Api
        ↓
Incident stored as Queued
        ↓
Analysis command sent to Service Bus
```

The frontend receives the created incident and navigates to its detail page.

The Worker processes the incident asynchronously in the backend. The frontend will later poll the API so status changes such as:

```text
Queued → Processing → Completed / Failed
```

appear automatically without requiring a manual page refresh.

---

## Pages

### Incidents

The Incident dashboard displays submitted incidents and provides a searchable overview of their current state.

Typical information includes:

- Incident title.
- Service.
- Environment.
- Severity.
- Status.
- Submission time.

Selecting an incident opens its detail page.

### Submit Incident

Allows engineers to submit a new technical incident.

The form captures information such as:

- Title.
- Description.
- Service.
- Environment.
- Severity.
- Symptoms.

Validation errors returned by the API are displayed against the relevant fields.

### Incident Detail

Displays the information associated with a single incident, including its current processing state.

This page will later also display:

- AI-generated summary.
- Likely causes.
- Recommended actions.
- Similar incidents.
- Supporting runbook evidence.

### Runbooks

Provides a list of operational Runbooks stored by IncidentIQ.

Users can open an existing Runbook or create a new one.

### Runbook Detail

Displays a Runbook's:

- Title.
- Description.
- Service.
- Content.
- Created/updated timestamps.

Users can also edit or delete the Runbook.

### Create / Edit Runbook

Both pages reuse the shared `RunbookForm` component.

This avoids duplicating the form fields, validation display, and submission layout between create and edit workflows.

---

## API Layer

Backend communication is kept in:

```text
src/api/
```

For example:

```text
incidentsApi.ts
runbooksApi.ts
```

These modules are responsible for:

- Calling the ASP.NET Core API.
- Serialising request data.
- Deserialising typed responses.
- Converting Problem Details responses into `ApiError`.

Pages therefore do not contain raw `fetch` logic throughout the application.

The API base URL is configured through:

```text
VITE_API_BASE_URL
```

with the local development API used as the fallback.

---

## Types

Shared frontend API/domain shapes are kept under:

```text
src/types/
```

For example:

```text
Incident
CreateIncidentRequest

Runbook
CreateRunbookRequest
UpdateRunbookRequest
```

Keeping these definitions separate from pages makes API interactions strongly typed and reusable.

---

## Routing

`App.tsx` defines the frontend routes.

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

`AppLayout` provides the shared navigation surrounding these pages.

---

## Local Development

Install dependencies:

```powershell
npm install
```

Start the Vite development server:

```powershell
npm run dev
```

The frontend normally runs at:

```text
http://localhost:5173
```

and communicates with the API configured by:

```env
VITE_API_BASE_URL=https://localhost:7156
```

The API and backend infrastructure can be run separately through Docker Compose.

---

## Build

Create a production build with:

```powershell
npm run build
```

Vite outputs the compiled frontend ready for static hosting.

---

## Design Approach

The frontend intentionally keeps its architecture straightforward:

- Pages own page-level state and loading behaviour.
- Reusable UI is extracted into components when it is genuinely shared.
- API calls are isolated from components.
- Request/response models are strongly typed.
- Backend services remain hidden behind the API.
- Styling stays close to the page or component it belongs to.
- Infrastructure and asynchronous processing remain backend concerns.

This keeps the frontend easy to navigate while leaving room for additional AI analysis and operational features as IncidentIQ grows.
