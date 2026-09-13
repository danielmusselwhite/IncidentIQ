# Grounded Incident Analysis

Grounded analysis combines two different evidence sources before asking Azure OpenAI to generate the Incident analysis.

## At a glance

```text
Incident
→ one embedding
→ retrieve similar historical Incidents ─┐
                                          ├─→ combined context
→ retrieve relevant Runbook chunks ──────┘
                                          ↓
                                AzureIncidentAnalyzer
                                          ↓
                            structured grounded analysis
                                          ↓
                           validate HI-* / RB-* references
                                          ↓
                      persist analysis + evidence snapshot
```

## Flow

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#ffffff"}} }%%
flowchart TB
    Handler["AnalyseIncidentHandler"]:::app --> Builder["IncidentAnalysisContextBuilder"]:::app
    Builder --> Embed["IEmbeddingGenerator<br/>one Incident embedding"]:::app
    Embed --> AIEmbed["Azure OpenAI<br/>embedding model"]:::ai

    Embed --> Historical["IHistoricalIncidentRetriever<br/>service + environment"]:::app
    Embed --> Runbooks["IRunbookChunkRetriever<br/>service"]:::app

    Historical --> HIVectors["Cosmos:<br/>HistoricalIncidentVectors"]:::data
    Runbooks --> RBChunks["Cosmos:<br/>RunbookChunks"]:::data

    Historical --> Context["Grounding context<br/>HI-* evidence"]:::app
    Runbooks --> Context
    Context --> Analyzer["IIncidentAnalyzer"]:::app
    Analyzer --> Chat["AzureIncidentAnalyzer<br/>structured output"]:::infra
    Chat --> AzureAI["Azure OpenAI<br/>chat model"]:::ai
    Chat --> Validate["evidence-reference validation"]:::app
    Validate --> Persist["IIncidentAnalysisStore<br/>analysis + evidence snapshot"]:::app

    classDef app fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef infra fill:#f3e8ff,stroke:#9333ea,color:#581c87,stroke-width:2px;
    classDef data fill:#ecfdf5,stroke:#059669,color:#064e3b,stroke-width:2px;
    classDef ai fill:#ede9fe,stroke:#7c3aed,color:#4c1d95,stroke-width:2px;
```

## Why two evidence types stay separate

Historical Incidents and Runbooks answer different questions:

- `HI-*` evidence says **what happened before** in similar Incidents.
- `RB-*` evidence says **what the organisation recommends doing**.

Keeping those sources distinct helps the model and the engineer avoid treating a Runbook as proof of a cause or a historical Incident as guaranteed repetition.

## Structured output and validation

`AzureIncidentAnalysisSchema` constrains the JSON shape produced by Azure OpenAI. After deserialisation, application-level validation checks semantic rules that a static schema cannot know in advance, especially whether returned `HI-*` and `RB-*` identifiers were actually present in this request's grounding context.

The completed analysis persists evidence snapshots alongside the result. That means the UI can show the evidence used at analysis time even if the vector index later changes.

See [RAG & AI Design](../RAG-AND-AI.md) for the shared AI rules.
