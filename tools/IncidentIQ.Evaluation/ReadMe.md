# IncidentIQ.Evaluation

`IncidentIQ.Evaluation` is a standalone tooling project used to evaluate the retrieval and grounded-AI behaviour of IncidentIQ against a controlled, version-controlled dataset.

The project exists to make AI behaviour measurable and repeatable rather than relying only on ad-hoc manual testing.

It currently focuses on retrieval evaluation and will later support citation/grounding checks and generated-analysis quality evaluation.

---

## What It Evaluates

The evaluation dataset contains:

- synthetic historical Incidents with stable IDs,
- synthetic Runbooks with stable IDs,
- operational-question scenarios,
- Incident-analysis scenarios,
- known expected historical-Incident evidence,
- known expected Runbook evidence,
- ambiguous/multi-source cases,
- semantically similar distractors,
- Service-filter cases,
- Environment-filter cases,
- no-evidence cases.

The dataset is intentionally small and deterministic so that changes in retrieval behaviour can be compared over time.

The current retrieval evaluation measures:

- `Precision@K`,
- `Recall@K`,
- returned rank,
- cosine distance,
- no-evidence behaviour,
- Service filtering,
- Environment filtering.

Historical Incident retrieval is evaluated at:

```text
P@1 / R@1
P@3 / R@3
```

Runbook retrieval is evaluated at:

```text
P@1 / R@1
P@3 / R@3
P@5 / R@5
```

No-evidence scenarios are evaluated separately rather than assigning artificial Precision/Recall values when there is no relevant evidence in the ground truth.

---

## How It Works

At a high level:

```text
Version-controlled evaluation dataset
        ↓
EvaluationDatasetLoader
        ↓
dataset validation
        ↓
real Azure OpenAI embeddings
text-embedding-3-small
        ↓
vectorise synthetic historical Incidents
and Runbook chunks
        ↓
isolated in-memory vector index
        ↓
run each evaluation scenario
        ↓
retrieve historical Incidents + Runbook chunks
        ↓
compare returned evidence against expected evidence
        ↓
Precision@K / Recall@K
rank / distance
no-evidence checks
        ↓
console report
```

The evaluator deliberately reuses production application components where practical, including:

- `IEmbeddingGenerator`,
- the Azure embedding implementation,
- Incident retrieval-input construction,
- Runbook chunking,
- Runbook embedding-text construction,
- historical-Incident and Runbook retrieval contracts.

This helps keep the evaluation path close to the real IncidentIQ retrieval pipeline.

### Why the Vector Index Is In Memory

The controlled corpus is vectorised using the real Azure OpenAI embedding deployment, but retrieval is currently performed against an isolated in-memory vector index.

This has several advantages:

- evaluation data cannot contaminate normal application Cosmos containers,
- runs are repeatable,
- the corpus is completely controlled,
- no additional evaluation-only Azure infrastructure is required,
- embedding/ranking quality can be measured independently from Cosmos configuration.

The in-memory retrievers use cosine distance and rank smaller distances as more similar.

This means the current evaluation primarily measures:

```text
embedding quality
+
query construction
+
metadata filtering
+
ranking quality
```

It does **not** yet prove exact parity with the production Cosmos vector engine, relevance threshold, vector index implementation, or Cosmos query behaviour.

Production-provider parity should therefore be treated as a separate integration concern.

---

## Evaluation Data

The controlled dataset lives under:

```text
tools/IncidentIQ.Evaluation/Data/
```

The main files are:

```text
historical-incidents.json
runbooks.json
evaluation-cases.json
```

Stable IDs are used so expected evidence can be declared explicitly in each test case.

The loader validates the dataset before evaluation begins, including checking that expected evidence IDs actually exist in the synthetic corpus.

---

## Retrieval Metrics

### Precision@K

Precision measures how much of the first `K` retrieved results is relevant.

```text
Precision@K =
relevant results in top K
-------------------------
            K
```

For example, if one of the first three returned historical Incidents is relevant:

```text
P@3 = 1 / 3 = 0.333
```

For Runbook chunks, retrieved chunks occupy retrieval slots independently. Multiple chunks from the same Runbook can therefore affect precision independently.

### Recall@K

Recall measures how much of the known relevant evidence was found in the first `K` results.

```text
Recall@K =
unique relevant evidence retrieved in top K
-------------------------------------------
total known relevant evidence
```

For example, if two of three expected historical Incidents are present in the first three results:

```text
R@3 = 2 / 3 = 0.667
```

### No-Evidence Cases

When the expected evidence set is empty, Recall is undefined.

These cases are therefore reported separately:

```text
No-evidence result: PASS
```

A no-evidence check passes when the configured metadata filters result in no evidence being returned.

---

## Current Baseline

A Stage 13B baseline run using the real Azure `text-embedding-3-small` deployment produced:

```text
Historical Incidents
  Mean P@1: 1.000
  Mean R@1: 0.870
  Mean P@3: 0.407
  Mean R@3: 0.963

Runbooks
  Mean P@1: 0.889
  Mean R@1: 0.833
  Mean P@3: 0.370
  Mean R@3: 1.000
  Mean P@5: 0.222
  Mean R@5: 1.000

No-evidence checks: 2/2 passed
```

These values should be interpreted in the context of the deliberately small evaluation corpus.

In particular, lower `Precision@3` / `Precision@5` values do not necessarily indicate poor retrieval when a filtered corpus contains only one known relevant source. The detailed per-case output should be inspected alongside aggregate metrics.

The baseline also exposed a useful ranking case in the Service-filter scenario: the expected Payment Gateway Runbook was retrieved at rank 2 rather than rank 1. This is the type of behaviour the evaluation project is intended to make visible.

---

## Prerequisites

Before running the evaluator, ensure you have:

- .NET 10 SDK,
- Azure CLI,
- access to the IncidentIQ Azure development subscription,
- the IncidentIQ Azure OpenAI development resource deployed,
- `Cognitive Services OpenAI User` access to the Azure OpenAI resource.

Authenticate locally:

```powershell
az login
```

Confirm the active subscription:

```powershell
az account show `
    --query "{Name:name,Subscription:id,Tenant:tenantId}" `
    --output table
```

The evaluator uses `DefaultAzureCredential`.

For predictable local behaviour, use the developer credential chain:

```powershell
$env:AZURE_TOKEN_CREDENTIALS = "dev"
```

---

## Configure User-Secrets

Initialise user-secrets once if required:

```powershell
dotnet user-secrets init `
    --project tools\IncidentIQ.Evaluation
```

Retrieve the current Azure OpenAI endpoint:

```powershell
az cognitiveservices account list `
    --resource-group "rg-incidentiq-dev" `
    --query "[?kind=='OpenAI'].properties.endpoint | [0]" `
    --output tsv
```

Configure the evaluation project:

```powershell
dotnet user-secrets set "AzureAI:Endpoint" "<AZURE_AI_ENDPOINT>" `
    --project tools\IncidentIQ.Evaluation

dotnet user-secrets set "AzureAI:DeploymentName" "incident-analysis" `
    --project tools\IncidentIQ.Evaluation

dotnet user-secrets set "AzureAI:ModelName" "gpt-5-mini" `
    --project tools\IncidentIQ.Evaluation

dotnet user-secrets set "AzureAI:Embedding:DeploymentName" "runbook-embedding" `
    --project tools\IncidentIQ.Evaluation

dotnet user-secrets set "AzureAI:Embedding:ModelName" "text-embedding-3-small" `
    --project tools\IncidentIQ.Evaluation

dotnet user-secrets set "AzureAI:Embedding:Dimensions" "1536" `
    --project tools\IncidentIQ.Evaluation
```

The retrieval evaluator only uses the embedding deployment during Stage 13B. The chat deployment/model settings are currently also configured because the shared Azure AI options object validates them.

Inspect the configured values with:

```powershell
dotnet user-secrets list `
    --project tools\IncidentIQ.Evaluation
```

Do not commit user-secret values.

---

## Run the Evaluation

From the repository root:

```powershell
$env:AZURE_TOKEN_CREDENTIALS = "dev"

dotnet build

dotnet run --project tools\IncidentIQ.Evaluation
```

The evaluator will:

1. load and validate the controlled dataset,
2. generate real Azure embeddings for the controlled corpus,
3. build the isolated in-memory vector index,
4. generate embeddings for each evaluation query,
5. retrieve historical-Incident and Runbook evidence,
6. compare retrieval results against expected evidence,
7. print detailed per-case metrics,
8. print aggregate retrieval metrics.

A successful run begins with output similar to:

```text
IncidentIQ evaluation dataset loaded successfully.
Historical Incidents: 8
Runbooks: 4
Evaluation cases: 10

Vectorising controlled evaluation corpus...
Indexed historical Incidents: 8
Indexed Runbook chunks: 4
```

Each case then reports ranked evidence:

```text
=== assistant-payment-gateway-timeout-001 — Payment gateway timeout in Production ===

Historical Incidents
  #1 10000000-...-0001 distance=0.2198 expected=True
  #2 10000000-...-0003 distance=0.4479 expected=False
  #3 10000000-...-0002 distance=0.4683 expected=False

  P@1=1.000  R@1=1.000
  P@3=0.333  R@3=1.000
```

The run ends with an aggregate summary:

```text
========================================
Retrieval Evaluation Summary
========================================

Historical Incidents
  Mean P@1: ...
  Mean R@1: ...
  Mean P@3: ...
  Mean R@3: ...

Runbooks
  Mean P@1: ...
  Mean R@1: ...
  Mean P@3: ...
  Mean R@3: ...
  Mean P@5: ...
  Mean R@5: ...

No-evidence checks: ...
```

---

## Troubleshooting

### Managed Identity / IMDS Errors

If a local run attempts to contact:

```text
169.254.169.254
```

the Azure credential chain is attempting Managed Identity, which is intended for Azure-hosted workloads.

For local evaluation use:

```powershell
$env:AZURE_TOKEN_CREDENTIALS = "dev"
```

and confirm that Azure CLI authentication works:

```powershell
az account get-access-token `
    --resource https://cognitiveservices.azure.com `
    --query expiresOn `
    --output tsv
```

### `PermissionDenied`

If Azure OpenAI reports that the principal lacks:

```text
Microsoft.CognitiveServices/accounts/OpenAI/deployments/embeddings/action
```

ensure the signed-in user has:

```text
Cognitive Services OpenAI User
```

on the IncidentIQ Azure OpenAI resource.

Azure RBAC changes can take a short time to propagate.

---

## Project Structure

At a high level:

```text
IncidentIQ.Evaluation/
├── Data/
│   ├── historical-incidents.json
│   ├── runbooks.json
│   └── evaluation-cases.json
├── Models/
│   └── evaluation and metric contracts
├── Retrieval/
│   ├── EvaluationCorpusIndexer
│   ├── EvaluationCorpusIndex
│   ├── InMemoryHistoricalIncidentRetriever
│   ├── InMemoryRunbookChunkRetriever
│   ├── RetrievalEvaluationRunner
│   ├── RetrievalMetricCalculator
│   ├── RetrievalConsoleReporter
│   └── CosineDistance
└── Program.cs
```

---

## Planned Evaluation Work

The evaluation project is intended to expand beyond retrieval.

Planned Stage 13 work includes:

```text
13B — Retrieval Evaluation
→ machine-readable retrieval report

13C — Citation & Grounding Evaluation
→ citation validity
→ evidence-reference validation
→ no invented citations

13D — Generated Analysis Quality
→ summary quality
→ likely-cause relevance
→ recommended-action relevance
→ uncertainty

13E — Evaluation Reporting
→ aggregate results
→ limitations
→ representative documented baseline
```

The goal is not to claim that a small synthetic benchmark fully represents production behaviour. The goal is to provide a repeatable engineering signal that makes retrieval and grounded-AI changes observable, comparable and easier to reason about.
