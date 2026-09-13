# IncidentIQ.Application

`IncidentIQ.Application` contains IncidentIQ's use cases, orchestration, validation, provider-independent models and external-service abstractions.

It defines **what the system needs to do**. Infrastructure defines **how Azure/Cosmos/Service Bus do it**.

## Dependency direction

```text
API / Worker
    ↓
Application
    ↓
Domain + Application contracts
    ↓
Application interfaces
    ↑ implemented by
Infrastructure
```

Application does not depend on Azure SDK types.

## Interface responsibilities

| Kind | Purpose | Examples |
| --- | --- | --- |
| Repository | Source/domain persistence | `IIncidentRepository`, `IRunbookRepository` |
| Store | Purpose-specific write | `IIncidentSubmissionStore`, `IIncidentAnalysisStore`, `IRunbookChunkStore`, `IHistoricalIncidentVectorStore` |
| Reader/Retriever | Purpose-specific read/search | `IIncidentAnalysisReader`, `IRunbookChunkRetriever`, `IHistoricalIncidentRetriever` |
| Queue | Publish a command | `IIncidentAnalysisQueue`, `IRunbookIndexQueue` |
| AI | Provider-independent AI capability | `IIncidentAnalyzer`, `IEmbeddingGenerator`, `IOperationalAssistant` |

This avoids turning source repositories into catch-all interfaces for transactions, vector search, messaging and AI.

## Incident analysis

The asynchronous analysis handler coordinates:

```text
AnalyseIncidentCommand
→ load Incident
→ mark Processing
→ build grounding context
→ IIncidentAnalyzer
→ validate evidence references
→ mark Completed
→ persist analysis + evidence snapshot
```

`IncidentAnalysisContextBuilder` generates one embedding, then retrieves:

- similar historical Incidents through `IHistoricalIncidentRetriever`,
- relevant Runbook chunks through `IRunbookChunkRetriever`.

The two evidence sets remain distinct and receive request-scoped `HI-*` / `RB-*` references.

## Historical Incident indexing

Completed Incidents are represented as `HistoricalIncidentVector` records containing the original Incident text/metadata plus an embedding.

The embedded text is based on title, description and symptoms rather than the previous AI analysis.

`IndexHistoricalIncidentHandler` owns the provider-independent indexing workflow; Cosmos persistence and Azure embeddings stay behind interfaces.

## Runbook indexing and retrieval

`RunbookChunker` creates deterministic overlapping chunks. `IndexRunbookHandler` embeds those chunks and writes them through `IRunbookChunkStore`.

Semantic retrieval is expressed through `IRunbookChunkRetriever` rather than `IRunbookRepository` because vector search operates over derived search data, not the editable source Runbook.

## Operational Assistant

The Assistant Application flow is:

```text
AskOperationalQuestionQuery
→ AskOperationalQuestionHandler
→ OperationalQuestionContextBuilder
→ IOperationalAssistant
→ evidence-reference validation
→ OperationalAssistantResult
```

`OperationalQuestionContextBuilder`:

- embeds the current question once,
- retrieves historical Incidents and Runbook chunks,
- applies optional service/environment filters,
- carries recent conversation history separately from grounding evidence.

Conversation history provides continuity only. It is not persisted by the backend and is not allowed to become evidence for later answers.

## Evidence validation

JSON schemas constrain model response shape in Infrastructure. Application then validates request-specific evidence references.

This separation is intentional:

```text
schema validation
→ "is the response structurally valid?"

Application validation
→ "does HI-2 / RB-1 actually exist in this request's evidence?"
```

For the full explanation, see [RAG & AI Design](../../docs/RAG-AND-AI.md).

## Testing

Application tests focus on:

- handler orchestration,
- validation,
- state transitions,
- indexing,
- grounding context construction,
- retrieval boundaries,
- evidence-reference validation,
- Assistant behaviour with mocked provider-independent interfaces.

No Azure OpenAI or Cosmos connection is required for these tests.
