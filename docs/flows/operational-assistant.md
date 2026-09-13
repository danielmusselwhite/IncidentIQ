# Operational Assistant

The Operational Assistant is a stateless backend conversation over fresh RAG retrieval. React owns the temporary conversation for the current browser session.

## At a glance

```text
React /assistant
→ POST /api/assistant/questions
→ AskOperationalQuestionHandler
→ OperationalQuestionContextBuilder
→ embed current question
→ retrieve historical Incidents + Runbook chunks
→ AzureOperationalAssistant
→ structured answer sections + HI-* / RB-* references
→ validate references
→ return answer + exact evidence to React
```

## Flow

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#ffffff"}} }%%
flowchart TB
    Web["React Operational Assistant<br/>ephemeral chat state"]:::web
    Web -->|"question + recent history + filters"| API["POST /api/assistant/questions"]:::host
    API --> Handler["AskOperationalQuestionHandler"]:::app
    Handler --> Builder["OperationalQuestionContextBuilder"]:::app

    Builder -->|"current question only"| Embed["IEmbeddingGenerator"]:::app
    Embed --> Historical["IHistoricalIncidentRetriever"]:::app
    Embed --> Runbooks["IRunbookChunkRetriever"]:::app

    Historical --> HIVectors["HistoricalIncidentVectors"]:::data
    Runbooks --> RBChunks["RunbookChunks"]:::data

    Historical --> Context["OperationalQuestionContext"]:::app
    Runbooks --> Context
    API -. "recent turns<br/>context only" .-> Context

    Context --> Assistant["IOperationalAssistant"]:::app
    Assistant --> Azure["AzureOperationalAssistant"]:::infra
    Azure --> AI["Azure OpenAI"]:::ai
    Azure --> Result["structured answer sections"]:::app
    Result --> Validate["validate HI-* / RB-*"]:::app
    Validate --> API
    API -->|"answer + exact evidence"| Web

    classDef web fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e,stroke-width:2px;
    classDef host fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px;
    classDef app fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef infra fill:#f3e8ff,stroke:#9333ea,color:#581c87,stroke-width:2px;
    classDef data fill:#ecfdf5,stroke:#059669,color:#064e3b,stroke-width:2px;
    classDef ai fill:#ede9fe,stroke:#7c3aed,color:#4c1d95,stroke-width:2px;
```

## Conversation versus evidence

The current question drives semantic retrieval. Recent user/Assistant turns are sent to the model so a follow-up such as *"what should I check first?"* still makes sense, but previous turns are not treated as evidence and cannot be cited.

The backend does not persist conversations yet. React sends the recent conversation history with each request. Persistence is intentionally deferred until authenticated user identity is available.

## Answer-scoped evidence

`HI-1` and `RB-1` are request-scoped identifiers. `HI-1` in one answer may refer to a different source than `HI-1` in a later answer.

The API therefore returns the evidence together with each answer. The React evidence inspector resolves a citation against the specific Assistant response that produced it, rather than against a global source list.

`AzureOperationalAssistantSchema` constrains the response shape, while application validation confirms that every cited reference was actually supplied for the current answer.
