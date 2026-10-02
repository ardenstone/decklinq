"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";
import { apiFetch, clearStoredToken, type Card, type Deck } from "@/lib/api";

export default function DeckPage() {
  const router = useRouter();
  const params = useParams<{ id: string }>();
  const deckId = Number(params.id);
  const [deck, setDeck] = useState<Deck | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({
    frontContent: "",
    backContent: "",
    hints: "",
    isLaTeX: false,
  });

  const loadDeck = async () => {
    try {
      const data = await apiFetch<Deck>(`/api/decks/${deckId}`);
      setDeck(data);
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
  };

  useEffect(() => {
    if (!Number.isFinite(deckId)) {
      router.replace("/dashboard");
      return;
    }

    void loadDeck();
  }, [deckId, router]);

  const handleAddCard = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setSubmitting(true);

    try {
      await apiFetch<Card>(`/api/decks/${deckId}/cards`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(form),
      });

      setForm({ frontContent: "", backContent: "", hints: "", isLaTeX: false });
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
                      <span className="text-xs uppercase tracking-[0.2em] text-cyan-300">Card {card.id}</span>
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
