# AI Evaluation

IncidentIQ includes a controlled evaluation framework for measuring retrieval and grounded AI behaviour.

The goal is not to claim that a small synthetic benchmark represents production performance. Instead, the framework provides a repeatable engineering signal for detecting changes in retrieval quality, grounding behaviour and generated analysis quality.

## Evaluation Approach

The evaluation framework uses a version-controlled synthetic corpus containing:

- 8 historical Incidents.
- 4 operational Runbooks.
- 10 evaluation scenarios.
- known expected evidence for each scenario.
- positive, ambiguous, filtered and no-evidence cases.
- semantically similar distractors.

The evaluator uses the same Azure AI embedding and generation implementations as the application where practical.

Retrieval is performed against an isolated in-memory vector index so the controlled evaluation corpus remains separate from application data.

## Retrieval Evaluation

Historical Incident and Runbook retrieval are evaluated using Precision@K and Recall@K.

The current baseline is:

| Source | Metric | Result |
| --- | --- | ---: |
| Historical Incidents | Precision@1 | 1.000 |
| Historical Incidents | Recall@1 | 0.870 |
| Historical Incidents | Precision@3 | 0.407 |
| Historical Incidents | Recall@3 | 0.963 |
| Runbooks | Precision@1 | 0.889 |
| Runbooks | Recall@1 | 0.833 |
| Runbooks | Precision@3 | 0.370 |
| Runbooks | Recall@3 | 1.000 |
| Runbooks | Precision@5 | 0.222 |
| Runbooks | Recall@5 | 1.000 |

Retrieval no-evidence checks passed `2/2`.

Lower Precision@K values at larger K are expected in parts of the deliberately small corpus where only one source is marked relevant. Per-case rankings are therefore retained alongside aggregate metrics.

The evaluation also intentionally preserves imperfect results. For example, one Service-filtered scenario retrieves the expected Payment Gateway Runbook at rank 2 rather than rank 1.

This is useful evaluation information rather than something to hide or tune away.

## Citation & Grounding Evaluation

Generated responses are evaluated separately from retrieval.

For each evaluation scenario, IncidentIQ:

1. retrieves controlled evidence;
2. supplies that evidence to the production AI generation implementation;
3. collects returned `HI-*` and `RB-*` evidence references;
4. verifies that every returned reference corresponds to evidence actually supplied to the model.

The current baseline produced:

| Metric | Result |
| --- | ---: |
| Evaluation cases | 10 |
| Citations returned | 35 |
| Valid citations | 35 |
| Invalid citations | 0 |
| Citation validity | 1.000 |
| Cases containing invalid citations | 0 |
| No-evidence citation checks | 1/1 passed |

This provides a deterministic check against invented evidence references.

Citation counts may vary between runs because generated responses are non-deterministic. Citation validity and grounding behaviour are therefore more meaningful than expecting identical generated output on every run.

## Generated Analysis Quality

Not every useful AI quality property can be measured deterministically.

Generated responses should also be reviewed against the following lightweight rubric:

| Criterion | Question |
| --- | --- |
| Summary quality | Does the response accurately summarise the operational problem? |
| Likely-cause relevance | Are suggested causes relevant to the Incident and supplied evidence? |
| Recommended-action relevance | Are the recommended actions useful and supported by the available context? |
| Uncertainty | Does the response avoid presenting weakly supported conclusions as certain? |
| Grounding | Are material conclusions consistent with the supplied evidence? |

Representative outputs are manually reviewed against these criteria rather than assigning artificial automated scores to subjective properties.

This deliberately separates:

- deterministic retrieval metrics;
- deterministic citation/reference validation;
- human judgement of generated-answer quality.

## Machine-Readable Reports

Each evaluation run produces a timestamped JSON report containing:

- embedding configuration;
- corpus metadata;
- per-case retrieval results;
- aggregate retrieval metrics;
- no-evidence results;
- citation results;
- aggregate citation-validity metrics.

Reports are generated under the evaluation project's build output and are not treated as permanent source-controlled benchmark artefacts.

## Limitations

The evaluation framework has several deliberate limitations.

The corpus is small and synthetic, so results should not be interpreted as production accuracy measurements.

Retrieval uses the real Azure embedding implementation but an isolated in-memory cosine-distance index. It therefore evaluates embedding, query construction, filtering and ranking behaviour without proving exact parity with the production Cosmos DB vector engine.

Generated responses are non-deterministic and may vary between runs.

Citation validity proves that returned references were supplied to the model. It does not by itself prove that every citation semantically supports every generated claim.

Semantic claim-level citation coverage and overall generated-answer quality require human judgement and are therefore kept separate from deterministic evaluation metrics.

## Running the Evaluation

From the repository root:

```powershell
$env:AZURE_TOKEN_CREDENTIALS = "dev"

dotnet run --project tools\IncidentIQ.Evaluation
```

Azure AI configuration is stored through .NET user-secrets and must not be committed to source control.

See [Evaluation ReadMe](../tools/IncidentIQ.Evaluation/ReadMe.md) for evaluator setup and implementation details.