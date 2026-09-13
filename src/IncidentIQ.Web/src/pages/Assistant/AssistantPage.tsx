import {
    useEffect,
    useMemo,
    useRef,
    useState,
    type FormEvent,
} from "react";
import { Link } from "react-router-dom";

import { askOperationalQuestion } from "../../api/assistantApi";
import { ApiError } from "../../api/apiError";

import type {
    AssistantConversationTurn,
    AssistantHistoricalIncidentEvidence,
    AssistantRunbookEvidence,
    OperationalAssistantResponse,
} from "../../types/assistant";

import "./AssistantPage.css";

interface UserMessage {
    id: string;
    role: "user";
    content: string;
}

interface AssistantMessage {
    id: string;
    role: "assistant";
    response: OperationalAssistantResponse;
}

type ChatMessage =
    | UserMessage
    | AssistantMessage;

interface SelectedEvidence {
    messageId: string;
    referenceId: string;
}

type SelectedEvidenceSource =
    | {
        kind: "historical-incident";
        evidence: AssistantHistoricalIncidentEvidence;
    }
    | {
        kind: "runbook";
        evidence: AssistantRunbookEvidence;
    };

const MAX_CONVERSATION_TURNS = 10;

/**
 * Provides the IncidentIQ Operational Assistant experience.
 *
 * Conversation state is intentionally kept in React only. Each request sends
 * recent conversation history back to the stateless API, while grounding
 * evidence remains scoped to the individual Assistant response that retrieved it.
 */
export default function AssistantPage() {
    const [messages, setMessages] =
        useState<ChatMessage[]>([]);

    const [question, setQuestion] =
        useState("");

    const [service, setService] =
        useState("");

    const [environment, setEnvironment] =
        useState("");

    const [isSubmitting, setIsSubmitting] =
        useState(false);

    const [error, setError] =
        useState<string | null>(null);

    const [selectedEvidence, setSelectedEvidence] =
        useState<SelectedEvidence | null>(null);

    const messagesEndRef =
        useRef<HTMLDivElement | null>(null);

    const canSubmit =
        question.trim().length > 0 &&
        !isSubmitting;

    const conversationHistory =
        useMemo(
            () => buildConversationHistory(messages),
            [messages],
        );

    const selectedEvidenceSource =
        useMemo(
            () =>
                resolveSelectedEvidence(
                    messages,
                    selectedEvidence,
                ),
            [messages, selectedEvidence],
        );

    /*
     * Keep the newest message or loading indicator visible without requiring
     * the user to manually scroll after every Assistant response.
     */
    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({
            behavior: "smooth",
            block: "nearest",
        });
    }, [messages, isSubmitting]);

    async function handleSubmit(
        event: FormEvent<HTMLFormElement>,
    ) {
        event.preventDefault();

        const trimmedQuestion =
            question.trim();

        if (!trimmedQuestion || isSubmitting) {
            return;
        }

        /*
         * History is captured before the new message is appended because the
         * API receives the current question separately from previous turns.
         */
        const previousConversationHistory =
            conversationHistory.slice(
                -MAX_CONVERSATION_TURNS,
            );

        const userMessage: UserMessage = {
            id: crypto.randomUUID(),
            role: "user",
            content: trimmedQuestion,
        };

        setMessages(current => [
            ...current,
            userMessage,
        ]);

        setQuestion("");
        setError(null);
        setIsSubmitting(true);

        try {
            const response =
                await askOperationalQuestion({
                    question: trimmedQuestion,
                    service:
                        service.trim() || null,
                    environment:
                        environment.trim() || null,
                    conversationHistory:
                        previousConversationHistory,
                });

            const assistantMessage: AssistantMessage = {
                id: crypto.randomUUID(),
                role: "assistant",
                response,
            };

            setMessages(current => [
                ...current,
                assistantMessage,
            ]);

            /*
             * Automatically inspect the first cited source in a new answer.
             * The user can select any other citation afterwards.
             */
            const firstReference =
                response.sections
                    .flatMap(
                        section =>
                            section.evidenceReferences,
                    )
                    .at(0);

            if (firstReference) {
                setSelectedEvidence({
                    messageId:
                        assistantMessage.id,
                    referenceId:
                        firstReference,
                });
            }
        } catch (exception) {
            if (exception instanceof ApiError) {
                setError(exception.message);
            } else {
                setError(
                    "IncidentIQ could not answer that question. Please try again.",
                );
            }
        } finally {
            setIsSubmitting(false);
        }
    }

    function clearConversation() {
        setMessages([]);
        setSelectedEvidence(null);
        setError(null);
        setQuestion("");
    }

    function handleEvidenceSelected(
        messageId: string,
        referenceId: string,
    ) {
        setSelectedEvidence({
            messageId,
            referenceId,
        });
    }

    return (
        <main className="assistant-page">
            <header className="assistant-page__header">
                <div>
                    <span className="assistant-page__eyebrow">
                        IncidentIQ AI
                    </span>

                    <h1>
                        Operational Assistant
                    </h1>

                    <p>
                        Investigate operational issues using
                        grounded evidence from historical
                        Incidents and Runbooks.
                    </p>
                </div>

                {messages.length > 0 && (
                    <button
                        type="button"
                        className="button button--secondary"
                        onClick={clearConversation}
                    >
                        Clear conversation
                    </button>
                )}
            </header>

            <section className="assistant-workspace">
                <div className="assistant-chat">
                    <div className="assistant-chat__filters">
                        <label>
                            <span>Service</span>

                            <input
                                type="text"
                                value={service}
                                onChange={event =>
                                    setService(
                                        event.target.value,
                                    )
                                }
                                placeholder="Any service"
                                disabled={isSubmitting}
                            />
                        </label>

                        <label>
                            <span>Environment</span>

                            <input
                                type="text"
                                value={environment}
                                onChange={event =>
                                    setEnvironment(
                                        event.target.value,
                                    )
                                }
                                placeholder="Any environment"
                                disabled={isSubmitting}
                            />
                        </label>

                        <div className="assistant-chat__scope-note">
                            <span className="assistant-chat__scope-dot" />

                            Filters scope evidence retrieval
                        </div>
                    </div>

                    <div className="assistant-chat__messages">
                        {messages.length === 0 ? (
                            <AssistantEmptyState
                                onPromptSelected={
                                    setQuestion
                                }
                            />
                        ) : (
                            messages.map(message => (
                                <ChatMessageView
                                    key={message.id}
                                    message={message}
                                    selectedEvidence={
                                        selectedEvidence
                                    }
                                    onEvidenceSelected={
                                        handleEvidenceSelected
                                    }
                                />
                            ))
                        )}

                        {isSubmitting && (
                            <div className="assistant-message assistant-message--assistant">
                                <div className="assistant-message__avatar">
                                    IQ
                                </div>

                                <div className="assistant-message__bubble assistant-message__bubble--loading">
                                    <span className="assistant-thinking-dot" />
                                    <span className="assistant-thinking-dot" />
                                    <span className="assistant-thinking-dot" />

                                    <span className="assistant-thinking-label">
                                        Retrieving evidence
                                    </span>
                                </div>
                            </div>
                        )}

                        <div ref={messagesEndRef} />
                    </div>

                    {error && (
                        <div
                            className="assistant-chat__error"
                            role="alert"
                        >
                            <strong>
                                Assistant request failed
                            </strong>

                            <span>{error}</span>
                        </div>
                    )}

                    <form
                        className="assistant-composer"
                        onSubmit={handleSubmit}
                    >
                        <textarea
                            value={question}
                            onChange={event =>
                                setQuestion(
                                    event.target.value,
                                )
                            }
                            onKeyDown={event => {
                                if (
                                    event.key ===
                                    "Enter" &&
                                    !event.shiftKey
                                ) {
                                    event.preventDefault();

                                    event.currentTarget
                                        .form
                                        ?.requestSubmit();
                                }
                            }}
                            placeholder="Ask about an operational issue..."
                            rows={3}
                            disabled={isSubmitting}
                        />

                        <div className="assistant-composer__footer">
                            <div className="assistant-composer__hint">
                                <span>
                                    Enter to send
                                </span>

                                <span>·</span>

                                <span>
                                    Shift + Enter for
                                    newline
                                </span>
                            </div>

                            <button
                                type="submit"
                                className="button"
                                disabled={!canSubmit}
                            >
                                {isSubmitting
                                    ? "Analysing..."
                                    : "Ask IncidentIQ"}
                            </button>
                        </div>
                    </form>
                </div>

                <EvidenceInspector
                    source={selectedEvidenceSource}
                />
            </section>
        </main>
    );
}

function AssistantEmptyState({
    onPromptSelected,
}: {
    onPromptSelected: (prompt: string) => void;
}) {
    const prompts = [
        "Why are payment requests timing out?",
        "What should I investigate when a service returns HTTP 504 responses?",
        "What Runbook guidance is available for payment gateway failures?",
    ];

    return (
        <div className="assistant-empty">
            <div
                className="assistant-empty__icon"
                aria-hidden="true"
            >
                IQ
            </div>

            <h2>
                How can I help investigate?
            </h2>

            <p>
                Ask about service failures, symptoms,
                previous Incidents or available operational
                guidance. IncidentIQ retrieves relevant
                evidence before answering.
            </p>

            <div className="assistant-empty__prompts">
                {prompts.map(prompt => (
                    <button
                        key={prompt}
                        type="button"
                        onClick={() =>
                            onPromptSelected(prompt)
                        }
                    >
                        <span>
                            {prompt}
                        </span>

                        <span
                            className="assistant-empty__prompt-arrow"
                            aria-hidden="true"
                        >
                            →
                        </span>
                    </button>
                ))}
            </div>
        </div>
    );
}

function ChatMessageView({
    message,
    selectedEvidence,
    onEvidenceSelected,
}: {
    message: ChatMessage;
    selectedEvidence: SelectedEvidence | null;
    onEvidenceSelected: (
        messageId: string,
        referenceId: string,
    ) => void;
}) {
    if (message.role === "user") {
        return (
            <div className="assistant-message assistant-message--user">
                <div className="assistant-message__bubble">
                    {message.content}
                </div>

                <div className="assistant-message__avatar">
                    DU
                </div>
            </div>
        );
    }

    return (
        <div className="assistant-message assistant-message--assistant">
            <div className="assistant-message__avatar">
                IQ
            </div>

            <div className="assistant-answer">
                <div className="assistant-answer__heading">
                    <span className="assistant-answer__label">
                        Grounded response
                    </span>

                    <span className="assistant-answer__source-count">
                        {countEvidence(
                            message.response,
                        )}{" "}
                        sources retrieved
                    </span>
                </div>

                {message.response.sections.map(
                    (section, index) => (
                        <section
                            key={index}
                            className="assistant-answer__section"
                        >
                            <p>
                                {section.content}
                            </p>

                            {section
                                .evidenceReferences
                                .length > 0 && (
                                    <div className="assistant-answer__citations">
                                        {section.evidenceReferences.map(
                                            reference => (
                                                <EvidenceReference
                                                    key={
                                                        reference
                                                    }
                                                    messageId={
                                                        message.id
                                                    }
                                                    reference={
                                                        reference
                                                    }
                                                    response={
                                                        message.response
                                                    }
                                                    isSelected={
                                                        selectedEvidence?.messageId ===
                                                        message.id &&
                                                        selectedEvidence?.referenceId ===
                                                        reference
                                                    }
                                                    onSelected={
                                                        onEvidenceSelected
                                                    }
                                                />
                                            ),
                                        )}
                                    </div>
                                )}
                        </section>
                    ),
                )}

                <footer className="assistant-answer__meta">
                    <span>
                        {message.response.model}
                    </span>

                    <span>
                        {formatDate(
                            message.response
                                .answeredAtUtc,
                        )}
                    </span>
                </footer>
            </div>
        </div>
    );
}

function EvidenceReference({
    messageId,
    reference,
    response,
    isSelected,
    onSelected,
}: {
    messageId: string;
    reference: string;
    response: OperationalAssistantResponse;
    isSelected: boolean;
    onSelected: (
        messageId: string,
        referenceId: string,
    ) => void;
}) {
    const historicalIncident =
        response.evidence.historicalIncidents.find(
            evidence =>
                evidence.referenceId ===
                reference,
        );

    const runbook =
        response.evidence.runbookChunks.find(
            evidence =>
                evidence.referenceId ===
                reference,
        );

    const isRunbook = Boolean(runbook);

    const title =
        historicalIncident?.title ??
        runbook?.title ??
        reference;

    return (
        <button
            type="button"
            className={[
                "assistant-citation",
                isRunbook
                    ? "assistant-citation--runbook"
                    : "",
                isSelected
                    ? "assistant-citation--selected"
                    : "",
            ]
                .filter(Boolean)
                .join(" ")}
            title={`Inspect ${title}`}
            aria-pressed={isSelected}
            onClick={() =>
                onSelected(
                    messageId,
                    reference,
                )
            }
        >
            <span>{reference}</span>

            <span
                className="assistant-citation__type"
                aria-hidden="true"
            >
                {isRunbook ? "RB" : "HI"}
            </span>
        </button>
    );
}

/**
 * Displays the source associated with the currently selected request-scoped
 * evidence reference.
 */
function EvidenceInspector({
    source,
}: {
    source: SelectedEvidenceSource | null;
}) {
    if (!source) {
        return (
            <aside className="assistant-evidence">
                <div className="assistant-evidence__header">
                    <div>
                        <span className="assistant-evidence__eyebrow">
                            Grounding
                        </span>

                        <h2>Evidence</h2>
                    </div>
                </div>

                <div className="assistant-evidence__empty">
                    <div className="assistant-evidence__empty-icon">
                        ↗
                    </div>

                    <h3>
                        Inspect a source
                    </h3>

                    <p>
                        Select an{" "}
                        <strong>HI-*</strong> or{" "}
                        <strong>RB-*</strong>{" "}
                        reference in an Assistant answer
                        to inspect the evidence used for
                        that response.
                    </p>
                </div>
            </aside>
        );
    }

    if (source.kind === "historical-incident") {
        return (
            <HistoricalIncidentInspector
                evidence={source.evidence}
            />
        );
    }

    return (
        <RunbookInspector
            evidence={source.evidence}
        />
    );
}

function HistoricalIncidentInspector({
    evidence,
}: {
    evidence: AssistantHistoricalIncidentEvidence;
}) {
    return (
        <aside className="assistant-evidence">
            <div className="assistant-evidence__header">
                <div>
                    <span className="assistant-evidence__eyebrow">
                        Historical Incident
                    </span>

                    <h2>{evidence.referenceId}</h2>
                </div>

                <span className="assistant-evidence__type-badge">
                    HI
                </span>
            </div>

            <div className="assistant-evidence__body">
                <section className="assistant-evidence__summary">
                    <h3>
                        {evidence.title}
                    </h3>

                    <div className="assistant-evidence__metadata">
                        <span>
                            {evidence.service}
                        </span>

                        <span>
                            {evidence.environment}
                        </span>

                        <span>
                            {evidence.severity}
                        </span>
                    </div>
                </section>

                <EvidenceSection title="Description">
                    <p>
                        {evidence.description}
                    </p>
                </EvidenceSection>

                <EvidenceSection title="Symptoms">
                    <p>
                        {evidence.symptoms ||
                            "No symptoms were recorded."}
                    </p>
                </EvidenceSection>

                <EvidenceSection title="Completed">
                    <p>
                        {formatDate(
                            evidence.completedAtUtc,
                        )}
                    </p>
                </EvidenceSection>
            </div>

            <div className="assistant-evidence__footer">
                <Link
                    to={`/incidents/${evidence.incidentId}`}
                    className="button assistant-evidence__link"
                >
                    View Incident
                    <span aria-hidden="true">
                        →
                    </span>
                </Link>
            </div>
        </aside>
    );
}

function RunbookInspector({
    evidence,
}: {
    evidence: AssistantRunbookEvidence;
}) {
    return (
        <aside className="assistant-evidence">
            <div className="assistant-evidence__header">
                <div>
                    <span className="assistant-evidence__eyebrow">
                        Runbook guidance
                    </span>

                    <h2>{evidence.referenceId}</h2>
                </div>

                <span className="assistant-evidence__type-badge assistant-evidence__type-badge--runbook">
                    RB
                </span>
            </div>

            <div className="assistant-evidence__body">
                <section className="assistant-evidence__summary">
                    <h3>
                        {evidence.title}
                    </h3>

                    <div className="assistant-evidence__metadata">
                        <span>
                            {evidence.service}
                        </span>

                        <span>
                            Chunk{" "}
                            {evidence.chunkIndex}
                        </span>
                    </div>
                </section>

                <EvidenceSection title="Relevant excerpt">
                    <p className="assistant-evidence__content">
                        {evidence.content}
                    </p>
                </EvidenceSection>
            </div>

            <div className="assistant-evidence__footer">
                <Link
                    to={`/runbooks/${evidence.runbookId}`}
                    className="button assistant-evidence__link"
                >
                    View Runbook
                    <span aria-hidden="true">
                        →
                    </span>
                </Link>
            </div>
        </aside>
    );
}

function EvidenceSection({
    title,
    children,
}: {
    title: string;
    children: React.ReactNode;
}) {
    return (
        <section className="assistant-evidence__section">
            <h4>{title}</h4>
            {children}
        </section>
    );
}

/**
 * Resolves a citation against the evidence belonging to the Assistant message
 * that produced it. This prevents request-scoped identifiers such as HI-1 from
 * accidentally resolving against evidence from another conversation turn.
 */
function resolveSelectedEvidence(
    messages: ChatMessage[],
    selected: SelectedEvidence | null,
): SelectedEvidenceSource | null {
    if (!selected) {
        return null;
    }

    const message =
        messages.find(
            candidate =>
                candidate.id ===
                selected.messageId &&
                candidate.role === "assistant",
        );

    if (!message || message.role !== "assistant") {
        return null;
    }

    const historicalIncident =
        message.response.evidence
            .historicalIncidents
            .find(
                evidence =>
                    evidence.referenceId ===
                    selected.referenceId,
            );

    if (historicalIncident) {
        return {
            kind: "historical-incident",
            evidence: historicalIncident,
        };
    }

    const runbook =
        message.response.evidence.runbookChunks.find(
            evidence =>
                evidence.referenceId ===
                selected.referenceId,
        );

    if (runbook) {
        return {
            kind: "runbook",
            evidence: runbook,
        };
    }

    return null;
}

/**
 * Converts rendered chat messages into the compact history accepted by the
 * stateless Assistant API.
 */
function buildConversationHistory(
    messages: ChatMessage[],
): AssistantConversationTurn[] {
    return messages.map(message => {
        if (message.role === "user") {
            return {
                role: "user",
                content: message.content,
            };
        }

        return {
            role: "assistant",
            content:
                message.response.sections
                    .map(
                        section =>
                            section.content,
                    )
                    .join("\n\n"),
        };
    });
}

function countEvidence(
    response: OperationalAssistantResponse,
) {
    return (
        response.evidence.historicalIncidents
            .length +
        response.evidence.runbookChunks.length
    );
}

function formatDate(value: string) {
    return new Intl.DateTimeFormat("en-GB", {
        dateStyle: "medium",
        timeStyle: "short",
    }).format(new Date(value));
}