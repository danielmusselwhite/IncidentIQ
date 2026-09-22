# AI Evaluation

IncidentIQ has a controlled evaluation project for retrieval and grounding behaviour.

## Dataset

- 8 synthetic historical Incidents.
- 4 synthetic Runbooks.
- 10 scenarios.
- Positive, ambiguous, filtered, distractor and no-evidence cases.
- Stable expected evidence IDs.

Real Azure embeddings are used; retrieval runs against an isolated in-memory cosine index so evaluation data does not affect application Cosmos data.

## Retrieval Baseline

| Source | Metric | Result |
| --- | --- | ---: |
| Historical Incidents | P@1 | 1.000 |
| Historical Incidents | R@1 | 0.870 |
| Historical Incidents | P@3 | 0.407 |
| Historical Incidents | R@3 | 0.963 |
| Runbooks | P@1 | 0.889 |
| Runbooks | R@1 | 0.833 |
| Runbooks | P@3 | 0.370 |
| Runbooks | R@3 | 1.000 |
| Runbooks | P@5 | 0.222 |
| Runbooks | R@5 | 1.000 |

No-evidence retrieval checks: **2/2 passed**.

## Citation Baseline

| Metric | Result |
| --- | ---: |
| Cases | 10 |
| Citations | 35 |
| Valid citations | 35 |
| Invalid citations | 0 |
| Citation validity | 1.000 |
| No-evidence citation checks | 1/1 passed |

Citation validity checks that references existed in supplied evidence. It does **not** prove that every citation semantically supports every claim.

## Human Review

Generated answers are reviewed for:
- summary quality,
- likely-cause relevance,
- recommended-action relevance,
- uncertainty,
- grounding.

Known limitation: the no-evidence case avoided fabricated citations/root-cause certainty but still produced generic diagnostic guidance. This is recorded rather than hidden.

## Limitations

- Small synthetic corpus; not a production accuracy claim.
- In-memory vector retrieval does not prove exact Cosmos vector-engine parity.
- Generated responses are non-deterministic.
- Semantic answer quality still requires human judgement.

## Run

```powershell
$env:AZURE_TOKEN_CREDENTIALS = "dev"
dotnet run --project tools\IncidentIQ.Evaluation
```

Configuration: [Evaluation README](../tools/IncidentIQ.Evaluation/ReadMe.md).
