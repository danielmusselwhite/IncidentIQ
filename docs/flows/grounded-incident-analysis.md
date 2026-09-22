# Grounded Incident Analysis

```text
Incident
→ one embedding
→ historical Incident retrieval ─┐
                                 ├→ grounding context
→ Runbook retrieval ─────────────┘
→ AzureIncidentAnalyzer
→ structured result
→ validate HI-* / RB-*
→ persist result + evidence snapshot
```

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#ffffff"}} }%%
flowchart TB
    Analyse["AnalyseIncidentHandler"]:::app --> Context["IncidentAnalysisContextBuilder"]:::app
    Context --> Embed["IEmbeddingGenerator"]:::app
    Embed --> HI["HistoricalIncidentRetriever"]:::app
    Embed --> RB["RunbookChunkRetriever"]:::app
    HI --> HIV["HistoricalIncidentVectors"]:::data
    RB --> RBC["RunbookChunks"]:::data
    HI --> Ground["Grounding context"]:::app
    RB --> Ground
    Ground --> Analyzer["AzureIncidentAnalyzer"]:::infra
    Analyzer --> AI["Azure OpenAI"]:::ai
    Analyzer --> Validate["Reference validation"]:::app
    Validate --> Persist["Analysis + evidence snapshot"]:::data

    classDef app fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef infra fill:#f3e8ff,stroke:#9333ea,color:#581c87,stroke-width:2px;
    classDef data fill:#ecfdf5,stroke:#059669,color:#064e3b,stroke-width:2px;
    classDef ai fill:#ede9fe,stroke:#7c3aed,color:#4c1d95,stroke-width:2px;
```

`HI-*` represents similar previous observations; `RB-*` represents operational guidance. Keeping them distinct avoids treating either as proof of the current cause.
