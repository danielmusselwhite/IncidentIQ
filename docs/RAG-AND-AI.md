# IncidentIQ RAG & AI Design

This document explains the AI flow without requiring knowledge of the Azure OpenAI SDK. The goal is to make the grounding and validation model easy to follow.

## The two grounding sources

IncidentIQ retrieves two types of evidence:

| Evidence | Meaning | Typical filter |
| --- | --- | --- |
| **Historical Incident (`HI-*`)** | Similar completed operational events | Service + environment |
| **Runbook chunk (`RB-*`)** | Relevant operational guidance | Service |

They remain separate because similarity is not proof of the current cause, and a Runbook is guidance rather than an observation of current system state.

## Grounded Incident analysis

The asynchronous Incident analysis pipeline is:

```text
Incident title + description + symptoms
        ↓
one embedding
        ↓
historical Incident retrieval ─┐
                               ├─→ IncidentAnalysisContext
Runbook chunk retrieval ───────┘
                               ↓
IIncidentAnalyzer
        ↓
AzureIncidentAnalyzer
        ↓
Azure OpenAI structured response
        ↓
semantic validation
        ↓
persist analysis + evidence snapshots
```

The same embedding is reused for both retrieval paths. Retrievals can run concurrently because they are independent.

The final persisted analysis contains the grounded result and snapshots of the evidence that was actually used. This is intentionally different from performing a fresh similarity search every time the page is opened.

## Operational Assistant

The Assistant uses the same evidence stores but has a synchronous request/response flow:

```text
current question
      ↓
embedding + retrieval
      ↓
OperationalQuestionContext
      ↓
IOperationalAssistant
      ↓
structured answer sections
      ↓
citation validation
      ↓
answer + evidence returned to React
```

Recent conversation history is also sent to the model for continuity. It is **not** included as grounding evidence and is not persisted by the backend at this stage.

The current question remains the primary retrieval input. This keeps semantic search predictable and avoids allowing a long conversation to drown out the engineer's latest question.

## Request-scoped evidence references

The model sees compact identifiers such as:

```text
HI-1
HI-2
RB-1
RB-2
```

Those identifiers only have meaning within the current analysis/answer.

This gives the generated text a simple way to refer back to evidence while keeping the Application contracts provider-independent.

## Structured output: schema versus semantic validation

IncidentIQ uses two levels of validation.

### 1. Azure OpenAI response schema

`AzureIncidentAnalysisSchema` constrains Incident-analysis output to the expected structured shape.

`AzureOperationalAssistantSchema` does the same for Assistant answer sections and their `evidenceReferences`.

The schema answers questions such as:

- Is the response valid JSON?
- Are required properties present?
- Is `evidenceReferences` an array of strings?

### 2. Application semantic validation

A JSON schema cannot know which references were retrieved for a particular request.

Application validation therefore checks that a generated reference such as `HI-3` or `RB-2` actually exists in the grounding context supplied to the model.

```text
schema-valid JSON
      ↓
deserialise Infrastructure response DTO
      ↓
map to Application model
      ↓
validate request-specific evidence references
```

This is why the valid evidence identifiers are not hard-coded into the static JSON schema.

## Treating retrieved content as untrusted data

Historical Incident descriptions, Runbook text and previous conversation turns may contain arbitrary text. The Azure prompts explicitly treat these values as **data, not instructions**.

The model is told not to follow instructions embedded inside retrieved content or conversation history, and not to claim access to logs, metrics, deployments or external systems unless those facts are explicitly present in the supplied evidence.

This does not make prompt injection impossible, but it provides an important separation between system instructions and retrieved user-controlled content.

## When no useful evidence is retrieved

The Assistant and Incident analyser are expected to communicate uncertainty rather than invent supporting evidence.

An answer section can contain an empty `evidenceReferences` collection when no supplied source materially supports that statement. The model must not manufacture an `HI-*` or `RB-*` reference.

## Azure AI failure boundary

The real Azure implementations use the same basic resilience model:

```text
Azure OpenAI SDK
→ small bounded transport retry / network timeout

IncidentIQ Azure AI adapter
→ overall request timeout
→ classify timeout / throttling / service / client / invalid response
→ log metadata, not prompt payloads
→ rethrow
```

For asynchronous Incident processing, Service Bus remains the durable outer retry mechanism. The synchronous Assistant returns failures through the API's normal Problem Details handling.

For runtime diagrams, see [`docs/flows/`](flows/README.md).
