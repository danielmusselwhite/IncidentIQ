# Incident Submission and Asynchronous Analysis

This is the main write path in IncidentIQ. The HTTP request creates durable work; the expensive AI analysis runs later in the Worker.

## At a glance

```text
React
→ POST /api/incidents
→ IncidentsController
→ CreateIncidentCommand
→ CreateIncidentHandler
→ Incident + outbox written atomically to Cosmos

                async boundary

Cosmos Change Feed
→ IncidentOutboxWorker
→ analyse-incident
→ AnalyseIncidentWorker
→ AnalyseIncidentHandler
→ grounded RAG analysis
→ completed Incident + analysis persisted
```

## Flow

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#ffffff"}} }%%
flowchart TB
    User["Engineer"]:::user -->|"submit"| Web["React Web"]:::web
    Web -->|"POST /api/incidents"| Controller["IncidentsController"]:::host
    Controller --> Command["CreateIncidentCommand"]:::app
    Command --> Handler["CreateIncidentHandler"]:::app
    Handler --> Store["IIncidentSubmissionStore"]:::app
    Store -->|"transactional batch"| Incidents["Cosmos: Incidents<br/>Incident + outbox"]:::data

    Incidents ==>|"Change Feed"| Relay["IncidentOutboxWorker"]:::host
    Relay --> QueuePort["IIncidentAnalysisQueue"]:::app
    QueuePort ==>|"AnalyseIncidentCommand"| Queue["Service Bus<br/>analyse-incident"]:::msg

    Queue ==>|"message"| Worker["AnalyseIncidentWorker"]:::host
    Worker --> Analyse["AnalyseIncidentHandler"]:::app
    Analyse --> RAG["Grounded analysis pipeline"]:::app
    RAG --> AI["Azure OpenAI"]:::ai
    Analyse -->|"completed state + analysis + evidence"| Incidents

    classDef user fill:#f8fafc,stroke:#64748b,color:#0f172a,stroke-width:2px;
    classDef web fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e,stroke-width:2px;
    classDef host fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px;
    classDef app fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef data fill:#ecfdf5,stroke:#059669,color:#064e3b,stroke-width:2px;
    classDef msg fill:#fff7ed,stroke:#d97706,color:#7c2d12,stroke-width:2px;
    classDef ai fill:#ede9fe,stroke:#7c3aed,color:#4c1d95,stroke-width:2px;
```

## Why the asynchronous split exists

The API does not wait for embeddings, retrieval or an LLM call. It only needs to durably create the Incident and its `AnalyseIncidentCommand` outbox record.

The transactional outbox prevents the classic dual-write failure where the Incident is stored successfully but publishing to Service Bus fails. Change Feed later relays the durable command to `analyse-incident`.

`AnalyseIncidentWorker` is a transport boundary, not the business workflow itself. It resolves `AnalyseIncidentHandler`, which coordinates state changes, retrieval, AI generation and persistence through Application interfaces.

For the RAG-specific part of the handler, see [Grounded Incident Analysis](grounded-incident-analysis.md).
