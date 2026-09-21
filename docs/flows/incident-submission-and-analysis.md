# Incident Submission and Analysis

```text
React
→ Bearer-authenticated POST /api/incidents
→ CreateIncidentHandler
→ Cosmos transactional batch: Incident + outbox

async

Change Feed
→ IncidentOutboxWorker
→ Service Bus: analyse-incident
→ AnalyseIncidentWorker
→ AnalyseIncidentHandler
→ grounded RAG analysis
→ Completed Incident + analysis/evidence
```

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#ffffff"}} }%%
flowchart TB
    Web["React Web"]:::web -->|"POST /api/incidents"| API["IncidentsController"]:::host
    API --> Create["CreateIncidentHandler"]:::app
    Create -->|"transactional batch"| Cosmos["Cosmos: Incident + outbox"]:::data

    Cosmos ==>|Change Feed| Relay["IncidentOutboxWorker"]:::host
    Relay ==>|AnalyseIncidentCommand| Bus["Service Bus"]:::msg
    Bus ==>|message| Worker["AnalyseIncidentWorker"]:::host
    Worker --> Analyse["AnalyseIncidentHandler"]:::app
    Analyse --> RAG["Grounded RAG"]:::app
    RAG --> AI["Azure OpenAI"]:::ai
    Analyse -->|"Completed + analysis + evidence"| Cosmos

    classDef web fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e,stroke-width:2px;
    classDef host fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px;
    classDef app fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef data fill:#ecfdf5,stroke:#059669,color:#064e3b,stroke-width:2px;
    classDef msg fill:#fff7ed,stroke:#d97706,color:#7c2d12,stroke-width:2px;
    classDef ai fill:#ede9fe,stroke:#7c3aed,color:#4c1d95,stroke-width:2px;
```

The transactional outbox removes the Cosmos/Service Bus dual-write gap. Service Bus then provides durable buffering, retries and DLQ behavior.
