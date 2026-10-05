"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { ChangeEvent, FormEvent, useCallback, useEffect, useState } from "react";
import {
  apiFetch,
  clearStoredToken,
  downloadApiFile,
  formatQuestionTypeLabel,
  getStoredToken,
  normalizeQuestionType,
  type Card,
  type CardMetadata,
  type Deck,
  type QuestionType,
} from "@/lib/api";

const apiBase = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

type MetadataFormState = {
  choices: Array<{ label: string; isCorrect: boolean }>;
  correctAnswer: boolean;
  imageUrl: string;
  blankWord: string;
  answer: string;
};

const createDefaultMetadata = (questionType: QuestionType = "basic"): MetadataFormState => {
  switch (questionType) {
    case "multiple-choice":
      return {
        choices: [
          { label: "", isCorrect: false },
          { label: "", isCorrect: false },
          { label: "", isCorrect: false },
        ],
        correctAnswer: true,
        imageUrl: "",
        blankWord: "",
        answer: "",
      };
    case "true-false":
      return {
        choices: [],
        correctAnswer: true,
        imageUrl: "",
        blankWord: "",
        answer: "",
      };
    case "image":
      return {
        choices: [],
        correctAnswer: true,
        imageUrl: "",
        blankWord: "",
        answer: "",
      };
    case "cloze":
      return {
        choices: [],
        correctAnswer: true,
        imageUrl: "",
        blankWord: "",
        answer: "",
      };
    case "basic":
    default:
      return {
        choices: [],
        correctAnswer: true,
        imageUrl: "",
        blankWord: "",
        answer: "",
      };
  }
};

const buildMetadataPayload = (questionType: QuestionType, metadata: MetadataFormState): CardMetadata | null => {
  switch (questionType) {
    case "multiple-choice": {
      const choices = metadata.choices
        .map((choice) => ({
          label: choice.label.trim(),
          isCorrect: Boolean(choice.isCorrect),
        }))
        .filter((choice) => choice.label.length > 0);

      return choices.length > 0 ? { choices } : null;
    }
    case "true-false":
      return { correctAnswer: Boolean(metadata.correctAnswer) };
    case "image":
      return metadata.imageUrl.trim() ? { imageUrl: metadata.imageUrl.trim() } : null;
    case "cloze": {
      const payload: CardMetadata = {};
      const blankWord = metadata.blankWord.trim();
      const answer = metadata.answer.trim();
      if (blankWord) {
        payload.blankWord = blankWord;
      }
      if (answer) {
        payload.answer = answer;
      }
      return Object.keys(payload).length > 0 ? payload : null;
    }
    case "basic":
    default:
      return null;
  }
};

const renderCardMetadataSummary = (card: Card) => {
  const questionType = normalizeQuestionType(card.questionType);

  switch (questionType) {
    case "multiple-choice": {
      const choices = card.metadata?.choices ?? [];
      if (choices.length === 0) {
        return null;
      }

      return (
        <div className="mt-4 rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-300">
          <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Choices</p>
          <ul className="mt-2 space-y-1">
            {choices.map((choice, index) => (
              <li key={`${choice.label}-${index}`} className={choice.isCorrect ? "text-emerald-300" : "text-slate-300"}>
                {choice.label} {choice.isCorrect ? "• correct" : ""}
              </li>
            ))}
          </ul>
        </div>
      );
    }
    case "true-false": {
      const correctAnswer = card.metadata?.correctAnswer ?? false;
      return (
        <div className="mt-4 rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-300">
          Correct answer: <span className="font-semibold text-cyan-200">{correctAnswer ? "True" : "False"}</span>
        </div>
      );
    }
    case "image": {
      const imageUrl = card.metadata?.imageUrl;
      if (!imageUrl) {
        return null;
      }

      return (
        <div className="mt-4 rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-300">
          <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Image</p>
          <a href={imageUrl} target="_blank" rel="noreferrer" className="mt-2 inline-block text-cyan-300 hover:text-cyan-200">
            {imageUrl}
          </a>
        </div>
      );
    }
    case "cloze": {
      const blankWord = card.metadata?.blankWord;
      const answer = card.metadata?.answer;
      return (
        <div className="mt-4 rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-300">
          {blankWord ? <div>Blank word: <span className="font-semibold text-cyan-200">{blankWord}</span></div> : null}
          {answer ? <div className="mt-1">Answer: <span className="font-semibold text-emerald-300">{answer}</span></div> : null}
        </div>
      );
    }
    case "basic":
    default:
      return null;
  }
};

export default function DeckPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const deckId = Number(params.id);
  const [deck, setDeck] = useState<Deck | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [status, setStatus] = useState<{ type: "success" | "error"; message: string } | null>(null);
  const [deckForm, setDeckForm] = useState({
    title: "",
    description: "",
    isPublic: false,
  });
  const [form, setForm] = useState(() => ({
    frontContent: "",
    backContent: "",
    hints: "",
    isLaTeX: false,
    questionType: "basic" as QuestionType,
    metadata: createDefaultMetadata("basic"),
  }));

  const loadDeck = useCallback(async () => {
    try {
      const data = await apiFetch<Deck>(`/api/decks/${deckId}`);
      setDeck(data);
      setDeckForm({
        title: data.title,
        description: data.description ?? "",
        isPublic: data.isPublic,
      });
    } catch (loadError) {
      if (loadError instanceof Error && loadError.message === "Unauthorized") {
        clearStoredToken();
        router.replace("/login");
        return;
      }

      setError(loadError instanceof Error ? loadError.message : "Unable to load deck.");
    } finally {
      setLoading(false);
    }
  }, [deckId, router]);

  useEffect(() => {
    if (!Number.isFinite(deckId)) {
      router.replace("/dashboard");
      return;
    }

    void (async () => {
      await loadDeck();
    })();
  }, [deckId, loadDeck, router]);

  const handleUpdateDeck = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setStatus(null);

    if (!deckForm.title.trim()) {
      setStatus({ type: "error", message: "Deck title is required." });
      return;
    }

    try {
      const updatedDeck = await apiFetch<Deck>(`/api/decks/${deckId}`, {
        method: "PUT",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({
          title: deckForm.title,
          description: deckForm.description,
          isPublic: deckForm.isPublic,
        }),
      });

      setDeck(updatedDeck);
      setStatus({ type: "success", message: "Deck updated." });
    } catch (updateError) {
      setStatus({
        type: "error",
        message: updateError instanceof Error ? updateError.message : "Unable to update deck.",
      });
    }
  };

  const handleDeleteDeck = async () => {
    if (!deck || !window.confirm(`Delete "${deck.title}"? This will remove the deck and its cards.`)) {
      return;
    }

    setError(null);
    setStatus(null);

    try {
      await apiFetch<void>(`/api/decks/${deckId}`, {
        method: "DELETE",
      });

      router.push("/dashboard");
    } catch (deleteError) {
      setStatus({
        type: "error",
        message: deleteError instanceof Error ? deleteError.message : "Unable to delete deck.",
      });
    }
  };

  const handleAddCard = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setSubmitting(true);

    try {
      const metadataPayload = buildMetadataPayload(form.questionType, form.metadata);
      const payload = {
        frontContent: form.frontContent,
        backContent: form.backContent,
        hints: form.hints || null,
        isLaTeX: form.isLaTeX,
        questionType: form.questionType,
        metadata: metadataPayload,
      };

      await apiFetch<Card>(`/api/decks/${deckId}/cards`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(payload),
      });

      setForm({
        frontContent: "",
        backContent: "",
        hints: "",
        isLaTeX: false,
        questionType: "basic",
        metadata: createDefaultMetadata("basic"),
      });
      await loadDeck();
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : "Unable to add card.");
    } finally {
      setSubmitting(false);
    }
  };

  const handleArchiveCard = async (cardId: number) => {
    setError(null);

    try {
      await apiFetch<Card>(`/api/decks/${deckId}/cards/${cardId}/archive`, {
        method: "POST",
      });

      await loadDeck();
    } catch (archiveError) {
      setError(archiveError instanceof Error ? archiveError.message : "Unable to archive card.");
    }
  };

  const handleExport = async (format: "json" | "csv") => {
    if (!deck) {
      return;
    }

    setStatus(null);
    setError(null);

    try {
      const safeFileName = deck.title.replace(/[^a-zA-Z0-9-_]+/g, "-").replace(/^-+|-+$/g, "") || `deck-${deck.id}`;
      await downloadApiFile(`/api/decks/${deckId}/export?format=${format}`, `${safeFileName}.${format}`);
      setStatus({ type: "success", message: `${format.toUpperCase()} export started.` });
    } catch (exportError) {
      setStatus({
        type: "error",
        message: exportError instanceof Error ? exportError.message : "Unable to export this deck.",
      });
    }
  };

  const handleImport = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    setStatus(null);
    setError(null);

    try {
      const format = file.name.toLowerCase().endsWith(".json") ? "json" : "csv";
      const content = await file.text();
      const token = getStoredToken();
      const response = await fetch(`${apiBase}/api/decks/${deckId}/import?format=${format}`, {
        method: "POST",
        headers: {
          Authorization: token ? `Bearer ${token}` : "",
          "Content-Type": format === "json" ? "application/json" : "text/csv",
        },
        body: content,
      });

      const payload = (await response.json().catch(() => null)) as { importedCardCount?: number; message?: string } | null;
      if (!response.ok) {
        throw new Error(payload?.message ?? "Unable to import this file.");
      }

      setStatus({
        type: "success",
        message: payload?.importedCardCount
          ? `Imported ${payload.importedCardCount} card${payload.importedCardCount === 1 ? "" : "s"}.`
          : "Deck import complete.",
      });
      event.target.value = "";
      await loadDeck();
    } catch (importError) {
      setStatus({
        type: "error",
        message: importError instanceof Error ? importError.message : "Unable to import card data.",
      });
    }
  };

  const renderMetadataFields = () => {
    switch (form.questionType) {
      case "multiple-choice":
        return (
          <div className="space-y-3">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Choices</p>
            {form.metadata.choices.map((choice, index) => (
              <div key={`choice-${index}`} className="flex items-center gap-2">
                <input
                  value={choice.label}
                  onChange={(event) => {
                    const nextChoices = [...form.metadata.choices];
                    nextChoices[index] = { ...nextChoices[index], label: event.target.value };
                    setForm((current) => ({ ...current, metadata: { ...current.metadata, choices: nextChoices } }));
                  }}
                  className="flex-1 rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-slate-100 outline-none focus:border-cyan-400"
                  placeholder={`Choice ${index + 1}`}
                />
                <label className="flex items-center gap-2 whitespace-nowrap text-xs text-slate-300">
                  <input
                    type="checkbox"
                    checked={choice.isCorrect}
                    onChange={(event) => {
                      const nextChoices = [...form.metadata.choices];
                      nextChoices[index] = { ...nextChoices[index], isCorrect: event.target.checked };
                      setForm((current) => ({ ...current, metadata: { ...current.metadata, choices: nextChoices } }));
                    }}
                    className="h-4 w-4 rounded border-slate-700 bg-slate-950"
                  />
                  Correct
                </label>
              </div>
            ))}
            <button
              type="button"
              onClick={() => setForm((current) => ({
                ...current,
                metadata: { ...current.metadata, choices: [...current.metadata.choices, { label: "", isCorrect: false }] },
              }))}
              className="rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-200 hover:bg-slate-800"
            >
              Add choice
            </button>
          </div>
        );
      case "true-false":
        return (
          <div className="space-y-2">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Correct answer</p>
            <div className="flex gap-4">
              <label className="flex items-center gap-2 text-sm text-slate-300">
                <input
                  type="radio"
                  checked={form.metadata.correctAnswer === true}
                  onChange={() => setForm((current) => ({ ...current, metadata: { ...current.metadata, correctAnswer: true } }))}
                  className="h-4 w-4"
                />
                True
              </label>
              <label className="flex items-center gap-2 text-sm text-slate-300">
                <input
                  type="radio"
                  checked={form.metadata.correctAnswer === false}
                  onChange={() => setForm((current) => ({ ...current, metadata: { ...current.metadata, correctAnswer: false } }))}
                  className="h-4 w-4"
                />
                False
              </label>
            </div>
          </div>
        );
      case "image":
        return (
          <div>
            <label className="mb-2 block text-sm text-slate-300" htmlFor="metadata-image-url">
              Image URL
            </label>
            <input
              id="metadata-image-url"
              value={form.metadata.imageUrl}
              onChange={(event) => setForm((current) => ({ ...current, metadata: { ...current.metadata, imageUrl: event.target.value } }))}
              className="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-slate-100 outline-none focus:border-cyan-400"
              placeholder="https://example.com/image.jpg"
            />
          </div>
        );
      case "cloze":
        return (
          <div className="space-y-4">
            <div>
              <label className="mb-2 block text-sm text-slate-300" htmlFor="metadata-blank-word">
                Blank word
              </label>
              <input
                id="metadata-blank-word"
                value={form.metadata.blankWord}
                onChange={(event) => setForm((current) => ({ ...current, metadata: { ...current.metadata, blankWord: event.target.value } }))}
                className="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-slate-100 outline-none focus:border-cyan-400"
                placeholder="Paris"
              />
            </div>
            <div>
              <label className="mb-2 block text-sm text-slate-300" htmlFor="metadata-answer">
                Answer
              </label>
              <input
                id="metadata-answer"
                value={form.metadata.answer}
                onChange={(event) => setForm((current) => ({ ...current, metadata: { ...current.metadata, answer: event.target.value } }))}
                className="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-slate-100 outline-none focus:border-cyan-400"
                placeholder="Paris"
              />
            </div>
          </div>
        );
      case "basic":
      default:
        return null;
    }
  };

  return (
    <main className="min-h-screen bg-slate-950 px-6 py-8 text-slate-100">
      <div className="mx-auto max-w-6xl">
        <header className="mb-8 flex items-center justify-between gap-4">
          <div>
            <Link href="/dashboard" className="text-sm text-cyan-300 hover:text-cyan-200">← Back to dashboard</Link>
            <h1 className="mt-3 text-3xl font-bold">{deck?.title ?? "Deck details"}</h1>
          </div>
          <div className="flex items-center gap-3">
            <Link
              href={deck ? `/decks/${deck.id}/study` : "/dashboard"}
              className="rounded-xl bg-cyan-400 px-4 py-2 text-sm font-semibold text-slate-950 transition hover:bg-cyan-300"
            >
              Start study
            </Link>
            <div className="rounded-full border border-slate-700 bg-slate-900/80 px-4 py-2 text-sm text-slate-200">
              {deck ? `${deck.cardCount} cards` : "Loading..."}
            </div>
          </div>
        </header>

        <div className="grid gap-8 lg:grid-cols-[0.9fr_1.1fr]">
          <div className="space-y-8">
            <section className="rounded-3xl border border-slate-800 bg-slate-900/80 p-6">
              <h2 className="text-xl font-semibold">Deck details</h2>
              <form className="mt-6 space-y-4" onSubmit={handleUpdateDeck}>
                <div>
                  <label className="mb-2 block text-sm text-slate-300" htmlFor="deck-title">
                    Title
                  </label>
                  <input
                    id="deck-title"
                    value={deckForm.title}
                    onChange={(event) => setDeckForm((current) => ({ ...current, title: event.target.value }))}
                    className="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-slate-100 outline-none focus:border-cyan-400"
                    placeholder="Deck name"
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm text-slate-300" htmlFor="deck-description">
                    Description
                  </label>
                  <textarea
                    id="deck-description"
                    rows={4}
                    value={deckForm.description}
                    onChange={(event) => setDeckForm((current) => ({ ...current, description: event.target.value }))}
                    className="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-slate-100 outline-none focus:border-cyan-400"
                    placeholder="What this deck covers"
                  />
                </div>

                <label className="flex items-center gap-3 text-sm text-slate-300">
                  <input
                    type="checkbox"
                    checked={deckForm.isPublic}
                    onChange={(event) => setDeckForm((current) => ({ ...current, isPublic: event.target.checked }))}
                    className="h-4 w-4 rounded border-slate-700 bg-slate-950"
                  />
                  Public deck
                </label>

                <div className="flex flex-wrap items-center gap-3">
                  <button
                    type="submit"
                    className="rounded-xl bg-cyan-400 px-4 py-2.5 font-semibold text-slate-950 transition hover:bg-cyan-300"
                  >
                    Save changes
                  </button>

                  <button
                    type="button"
                    onClick={() => void handleDeleteDeck()}
                    className="rounded-xl border border-rose-500/50 bg-rose-500/10 px-4 py-2.5 font-semibold text-rose-200 transition hover:bg-rose-500/20"
                  >
                    Delete deck
                  </button>
                </div>
              </form>
            </section>

            <section className="rounded-3xl border border-slate-800 bg-slate-900/80 p-6">
              <h2 className="text-xl font-semibold">Add a card</h2>
              <form className="mt-6 space-y-4" onSubmit={handleAddCard}>
                <div>
                  <label className="mb-2 block text-sm text-slate-300" htmlFor="frontContent">
                    Front side
                  </label>
                  <textarea
                    id="frontContent"
                    rows={4}
                    value={form.frontContent}
                    onChange={(event) => setForm((current) => ({ ...current, frontContent: event.target.value }))}
                    className="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-slate-100 outline-none focus:border-cyan-400"
                    placeholder="What is the capital of France?"
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm text-slate-300" htmlFor="backContent">
                    Back side
                  </label>
                  <textarea
                    id="backContent"
                    rows={4}
                    value={form.backContent}
                    onChange={(event) => setForm((current) => ({ ...current, backContent: event.target.value }))}
                    className="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-slate-100 outline-none focus:border-cyan-400"
                    placeholder="Paris"
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm text-slate-300" htmlFor="questionType">
                    Question type
                  </label>
                  <select
                    id="questionType"
                    value={form.questionType}
                    onChange={(event) => {
                      const nextQuestionType = event.target.value as QuestionType;
                      setForm((current) => ({
                        ...current,
                        questionType: nextQuestionType,
                        metadata: createDefaultMetadata(nextQuestionType),
                      }));
                    }}
                    className="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-slate-100 outline-none focus:border-cyan-400"
                  >
                    <option value="basic">Basic (front/back)</option>
                    <option value="true-false">True / False</option>
                    <option value="multiple-choice">Multiple choice</option>
                    <option value="cloze">Fill-in-the-blank (cloze)</option>
                    <option value="image">Image</option>
                  </select>
                </div>

                {renderMetadataFields()}

                <div>
                  <label className="mb-2 block text-sm text-slate-300" htmlFor="hints">
                    Hint (optional)
                  </label>
                  <input
                    id="hints"
                    value={form.hints}
                    onChange={(event) => setForm((current) => ({ ...current, hints: event.target.value }))}
                    className="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-slate-100 outline-none focus:border-cyan-400"
                    placeholder="Think of the Eiffel Tower"
                  />
                </div>

                <label className="flex items-center gap-3 text-sm text-slate-300">
                  <input
                    type="checkbox"
                    checked={form.isLaTeX}
                    onChange={(event) => setForm((current) => ({ ...current, isLaTeX: event.target.checked }))}
                    className="h-4 w-4 rounded border-slate-700 bg-slate-950"
                  />
                  LaTeX content
                </label>

                {error ? (
                  <div className="rounded-xl border border-rose-500/40 bg-rose-500/10 px-3 py-2 text-sm text-rose-200">
                    {error}
                  </div>
                ) : null}

                <button
                  type="submit"
                  disabled={submitting || !form.frontContent.trim() || !form.backContent.trim()}
                  className="w-full rounded-xl bg-cyan-400 px-4 py-3 font-semibold text-slate-950 transition hover:bg-cyan-300 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {submitting ? "Adding card..." : "Add card"}
                </button>
              </form>
            </section>
          </div>

          <section className="rounded-3xl border border-slate-800 bg-slate-900/80 p-6">
            <div className="flex items-center justify-between gap-3">
              <h2 className="text-xl font-semibold">Import / export</h2>
            </div>

            <div className="mt-4 grid gap-3 sm:grid-cols-2">
              <button
                type="button"
                onClick={() => void handleExport("json")}
                className="rounded-xl border border-cyan-500/50 bg-cyan-500/10 px-3 py-2 text-sm font-medium text-cyan-200 transition hover:bg-cyan-500/20"
              >
                Export JSON
              </button>
              <button
                type="button"
                onClick={() => void handleExport("csv")}
                className="rounded-xl border border-violet-500/50 bg-violet-500/10 px-3 py-2 text-sm font-medium text-violet-200 transition hover:bg-violet-500/20"
              >
                Export CSV
              </button>
            </div>

            <div className="mt-4 rounded-2xl border border-dashed border-slate-700 bg-slate-950/60 p-4">
              <label className="block text-sm font-medium text-slate-200" htmlFor="deck-import-file">
                Import a CSV or JSON file
              </label>
              <input
                id="deck-import-file"
                type="file"
                accept=".csv,.json,text/csv,application/json"
                onChange={handleImport}
                className="mt-3 block w-full text-sm text-slate-300 file:mr-4 file:rounded-xl file:border-0 file:bg-cyan-400 file:px-3 file:py-2 file:text-sm file:font-semibold file:text-slate-950"
              />
            </div>

            {status ? (
              <div
                className={`mt-4 rounded-xl border px-3 py-2 text-sm ${
                  status.type === "success"
                    ? "border-emerald-500/40 bg-emerald-500/10 text-emerald-200"
                    : "border-rose-500/40 bg-rose-500/10 text-rose-200"
                }`}
              >
                {status.message}
              </div>
            ) : null}
          </section>

          <section className="rounded-3xl border border-slate-800 bg-slate-900/80 p-6">
            <h2 className="text-xl font-semibold">Card library</h2>

            {loading ? (
              <div className="mt-6 rounded-2xl border border-dashed border-slate-700 bg-slate-950/50 p-8 text-slate-400">
                Loading deck cards...
              </div>
            ) : deck && deck.cards.length > 0 ? (
              <div className="mt-6 space-y-4">
                {deck.cards.map((card) => (
                  <div key={card.id} className="rounded-2xl border border-slate-700 bg-slate-950/60 p-4">
                    <div className="flex items-center justify-between gap-4">
                      <div className="flex items-center gap-2">
                        <span className="text-xs uppercase tracking-[0.2em] text-cyan-300">Card {card.id}</span>
                        {normalizeQuestionType(card.questionType) !== "basic" ? (
                          <span className="rounded-full border border-cyan-500/40 bg-cyan-500/10 px-2 py-1 text-[10px] uppercase tracking-[0.2em] text-cyan-200">
                            {formatQuestionTypeLabel(card.questionType)}
                          </span>
                        ) : null}
                      </div>
                      {card.isLaTeX ? <span className="rounded-full border border-cyan-500/40 bg-cyan-500/10 px-2 py-1 text-xs text-cyan-200">LaTeX</span> : null}
                    </div>
                    <div className="mt-4 grid gap-3 md:grid-cols-2">
                      <div>
                        <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Front</p>
                        <p className="mt-2 text-slate-100">{card.frontContent}</p>
                      </div>
                      <div>
                        <p className="text-xs uppercase tracking-[0.2em] text-slate-500">Back</p>
                        <p className="mt-2 text-slate-100">{card.backContent}</p>
                      </div>
                    </div>
                    {renderCardMetadataSummary(card)}
                    {card.hints ? (
                      <div className="mt-4 rounded-xl border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-slate-300">
                        Hint: {card.hints}
                      </div>
                    ) : null}

                    <div className="mt-4 flex justify-end">
                      <button
                        type="button"
                        onClick={() => handleArchiveCard(card.id)}
                        className="rounded-xl border border-amber-500/50 bg-amber-500/10 px-3 py-2 text-sm font-medium text-amber-200 transition hover:bg-amber-500/20"
                      >
                        Archive
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="mt-6 rounded-2xl border border-dashed border-slate-700 bg-slate-950/50 p-8 text-slate-400">
                This deck is empty. Add your first card to start memorizing.
              </div>
            )}
          </section>
        </div>
      </div>
    </main>
  );
}
