# RAG & AI Design

IncidentIQ grounds AI output in two evidence types:

| Evidence | Purpose | Filter |
| --- | --- | --- |
| `HI-*` Historical Incident | Similar previous incidents | Service + environment |
| `RB-*` Runbook chunk | Operational guidance | Service |

They remain separate because similarity is not proof of cause, and a Runbook is guidance rather than current-state evidence.

## Incident Analysis

```text
Incident title + description + symptoms
→ one embedding
→ retrieve historical Incidents + Runbook chunks
→ build grounding context
→ AzureIncidentAnalyzer
→ structured response
→ validate HI-* / RB-* references
→ persist analysis + evidence snapshot
```

## Operational Assistant

```text
current question
→ embedding + retrieval
→ grounded context
→ AzureOperationalAssistant
→ structured answer sections
→ validate references
→ return answer + exact evidence
```

Recent conversation history is sent for continuity but:
- is not used as grounding evidence,
- is not persisted by the backend,
- cannot be cited as evidence.

## Validation

- **JSON schema:** validates output shape.
- **Application validation:** checks that each returned `HI-*` / `RB-*` reference was actually supplied for that request.

Evidence IDs are request-scoped; `HI-1` in one response is unrelated to `HI-1` in another.

## Prompt Safety

Retrieved Incident text, Runbook content and prior conversation turns are treated as untrusted data. Prompts instruct the model not to follow embedded instructions or claim access to systems not represented in supplied evidence.

## Failure Handling

Azure AI adapters use:
- bounded SDK retries,
- network/request timeouts,
- failure classification,
- metadata-only logging.

Service Bus remains the durable outer retry mechanism for asynchronous Incident analysis.
