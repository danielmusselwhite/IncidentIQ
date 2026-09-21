# Operational Assistant

```text
React
→ authenticated POST /api/assistant/questions
→ AskOperationalQuestionHandler
→ embed current question
→ retrieve historical Incidents + Runbook chunks
→ AzureOperationalAssistant
→ structured answer + HI-* / RB-*
→ validate references
→ return answer + exact evidence
```

```mermaid
%%{init: {"theme":"base","themeVariables":{"background":"#ffffff"}} }%%
flowchart TB
    Web["React Assistant<br/>browser conversation state"]:::web --> API["AssistantController"]:::host
    API --> Handler["AskOperationalQuestionHandler"]:::app
    Handler --> Context["OperationalQuestionContextBuilder"]:::app
    Context --> Embed["IEmbeddingGenerator"]:::app
    Embed --> HI["Historical Incident retrieval"]:::app
    Embed --> RB["Runbook retrieval"]:::app
    HI --> Ground["Grounding context"]:::app
    RB --> Ground
    Ground --> Assistant["AzureOperationalAssistant"]:::infra
    Assistant --> AI["Azure OpenAI"]:::ai
    Assistant --> Validate["Reference validation"]:::app
    Validate --> Web

    classDef web fill:#e0f2fe,stroke:#0284c7,color:#0c4a6e,stroke-width:2px;
    classDef host fill:#dbeafe,stroke:#2563eb,color:#1e3a8a,stroke-width:2px;
    classDef app fill:#dcfce7,stroke:#16a34a,color:#14532d,stroke-width:2px;
    classDef infra fill:#f3e8ff,stroke:#9333ea,color:#581c87,stroke-width:2px;
    classDef ai fill:#ede9fe,stroke:#7c3aed,color:#4c1d95,stroke-width:2px;
```

The current question drives retrieval. Recent conversation turns provide continuity only and are not grounding evidence. Evidence identifiers are scoped to the response that returned them.
