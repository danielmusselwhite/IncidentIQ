import { useMemo, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";

import { askOperationalQuestion } from "../../api/assistantApi";
import type {
    AssistantConversationTurn,
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

const MAX_CONVERSATION_TURNS = 10;

/**
 * Provides an ephemeral conversational interface for grounded operational
 * questions. Conversation state remains in the browser and is not persisted.
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

    const canSubmit =
        question.trim().length > 0 &&
        !isSubmitting;

    const conversationHistory =
        useMemo(
            () => buildConversationHistory(messages),
            [messages],
        );

    async function handleSubmit(
        event: FormEvent<HTMLFormElement>,
    ) {
        event.preventDefault();

        const trimmedQuestion =
            question.trim();

        if (!trimmedQuestion || isSubmitting) {
            return;
        }

        const userMessage: UserMessage = {
            id: crypto.randomUUID(),
            role: "user",
            content: trimmedQuestion,
        };

        /*
         * Capture the history before adding the current question.
         * The API accepts the current question separately from previous turns.
         */
        const previousConversationHistory =
            conversationHistory.slice(
                -MAX_CONVERSATION_TURNS,
            );

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
        } catch {
            setError(
                "IncidentIQ could not answer that question. Please try again.",
            );
        } finally {
            setIsSubmitting(false);
        }
    }

    function clearConversation() {
        setMessages([]);
        setError(null);
    }

    return (
        <main className="assistant-page">
            <header className="assistant-page__header">
                <div>
                    <span className="assistant-page__eyebrow">
                        IncidentIQ AI
                    </span>

                    <h1>Operational Assistant</h1>

                    <p>
                        Investigate operational issues using
                        historical Incidents and Runbook guidance.
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
                            />
                        </label>
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
                                </div>
                            </div>
                        )}
                    </div>

                    {error && (
                        <div
                            className="assistant-chat__error"
                            role="alert"
                        >
                            {error}
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
                            placeholder="Ask about an operational issue..."
                            rows={3}
                            disabled={isSubmitting}
                        />

                        <div className="assistant-composer__footer">
                            <span>
                                Answers are grounded in retrieved
                                Incident and Runbook evidence.
                            </span>

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

            <h2>How can I help investigate?</h2>

            <p>
                Ask about service failures, symptoms,
                previous Incidents or available recovery
                guidance.
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
                        {prompt}
                    </button>
                ))}
            </div>
        </div>
    );
}

function ChatMessageView({
    message,
}: {
    message: ChatMessage;
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
                {message.response.sections.map(
                    (section, index) => (
                        <section
                            key={index}
                            className="assistant-answer__section"
                        >
                            <p>{section.content}</p>

                            {section.evidenceReferences.length >
                                0 && (
                                    <div className="assistant-answer__citations">
                                        {section.evidenceReferences.map(
                                            reference => (
                                                <EvidenceReference
                                                    key={
                                                        reference
                                                    }
                                                    reference={
                                                        reference
                                                    }
                                                    response={
                                                        message.response
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
    reference,
    response,
}: {
    reference: string;
    response: OperationalAssistantResponse;
}) {
    const historicalIncident =
        response.evidence.historicalIncidents.find(
            item => item.referenceId === reference,
        );

    if (historicalIncident) {
        return (
            <Link
                to={`/incidents/${historicalIncident.incidentId}`}
                className="assistant-citation"
                title={historicalIncident.title}
            >
                {reference}
            </Link>
        );
    }

    const runbook =
        response.evidence.runbookChunks.find(
            item => item.referenceId === reference,
        );

    if (runbook) {
        return (
            <Link
                to={`/runbooks/${runbook.runbookId}`}
                className="assistant-citation assistant-citation--runbook"
                title={runbook.title}
            >
                {reference}
            </Link>
        );
    }

    return (
        <span className="assistant-citation">
            {reference}
        </span>
    );
}

/**
 * Converts the rendered chat into the compact conversation history accepted
 * by the stateless Assistant API.
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
            content: message.response.sections
                .map(section => section.content)
                .join("\n\n"),
        };
    });
}

function formatDate(value: string) {
    return new Intl.DateTimeFormat("en-GB", {
        dateStyle: "medium",
        timeStyle: "short",
    }).format(new Date(value));
}