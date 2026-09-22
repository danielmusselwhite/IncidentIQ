# IncidentIQ.Evaluation

Standalone evaluation tooling for retrieval and grounded-AI behavior.

## Scope

- Historical Incident retrieval.
- Runbook retrieval.
- Service/environment filtering.
- No-evidence cases.
- `HI-*` / `RB-*` citation validity.
- Human review of generated answer quality.

The controlled corpus is synthetic/versioned. Real Azure embeddings are used; vector retrieval is isolated in memory.

## Metrics

Historical Incidents:

```text
P@1 / R@1
P@3 / R@3
```

Runbooks:

```text
P@1 / R@1
P@3 / R@3
P@5 / R@5
```

Current baseline:

```text
Historical: P@1 1.000, R@1 0.870, P@3 0.407, R@3 0.963
Runbooks:   P@1 0.889, R@1 0.833, P@3 0.370, R@3 1.000, P@5 0.222, R@5 1.000
No-evidence retrieval: 2/2
Citation validity: 35/35 (1.000)
```

## Data

```text
Data/
├── historical-incidents.json
├── runbooks.json
└── evaluation-cases.json
```

## Configure

```powershell
az login
$env:AZURE_TOKEN_CREDENTIALS = "dev"

dotnet user-secrets set "AzureAI:Endpoint" "<AZURE_AI_ENDPOINT>" --project tools\IncidentIQ.Evaluation
dotnet user-secrets set "AzureAI:DeploymentName" "incident-analysis" --project tools\IncidentIQ.Evaluation
dotnet user-secrets set "AzureAI:ModelName" "gpt-5-mini" --project tools\IncidentIQ.Evaluation
dotnet user-secrets set "AzureAI:Embedding:DeploymentName" "runbook-embedding" --project tools\IncidentIQ.Evaluation
dotnet user-secrets set "AzureAI:Embedding:ModelName" "text-embedding-3-small" --project tools\IncidentIQ.Evaluation
dotnet user-secrets set "AzureAI:Embedding:Dimensions" "1536" --project tools\IncidentIQ.Evaluation
```

The signed-in user needs `Cognitive Services OpenAI User`.

## Run

```powershell
dotnet run --project tools\IncidentIQ.Evaluation
```

Outputs include machine-readable metrics plus a human-review report.

Limitations and baseline interpretation: [AI Evaluation](../../docs/AI-EVALUATION.md).
