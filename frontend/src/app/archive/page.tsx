"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { apiFetch, clearStoredToken, type Card, type Deck } from "@/lib/api";

export default function ArchivePage() {
  const router = useRouter();
  const [decks, setDecks] = useState<Deck[]>([]);
  const [archivedCards, setArchivedCards] = useState<Card[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [restoreTargets, setRestoreTargets] = useState<Record<number, string>>({});

  const loadArchive = async () => {
    try {
      const [deckData, archivedCardsData] = await Promise.all([
        apiFetch<Deck[]>("/api/decks"),
        apiFetch<Card[]>("/api/decks/archive"),
      ]);

      setDecks(deckData);
      setArchivedCards(archivedCardsData);
      setRestoreTargets((current) => {
        const next = { ...current };
        for (const card of archivedCardsData) {
          if (!next[card.id]) {
            const initialDeck = deckData[0]?.id ? String(deckData[0].id) : "";
            next[card.id] = initialDeck;
          }
        }
        return next;
      });
    } catch (loadError) {
      if (loadError instanceof Error && loadError.message === "Unauthorized") {
        clearStoredToken();
        router.replace("/login");
        return;
      }

      setError(loadError instanceof Error ? loadError.message : "Unable to load archive.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadArchive();
  }, [router]);

  const handleRestore = async (cardId: number) => {
    const targetDeckId = Number(restoreTargets[cardId]);
    if (!targetDeckId) {
      setError("Choose a deck before restoring this card.");
      return;
    }

    setError(null);

    try {
      await apiFetch<Card>(`/api/decks/archive/${cardId}/restore`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ deckId: targetDeckId }),
      });

      await loadArchive();
    } catch (restoreError) {
      setError(restoreError instanceof Error ? restoreError.message : "Unable to restore card.");
    }
  };

  const handlePermanentDelete = async (cardId: number) => {
    setError(null);

    try {
      await apiFetch<void>(`/api/decks/archive/${cardId}`, {
        method: "DELETE",
      });

      await loadArchive();
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : "Unable to delete card permanently.");
    }
  };

  return (
    <main className="min-h-screen bg-slate-950 px-6 py-8 text-slate-100">
      <div className="mx-auto max-w-6xl">
        <header className="mb-8 flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div>
            <Link href="/dashboard" className="text-sm text-cyan-300 hover:text-cyan-200">← Back to dashboard</Link>
            <h1 className="mt-3 text-3xl font-bold">Archive</h1>
          </div>
          <div className="rounded-full border border-slate-700 bg-slate-900/80 px-4 py-2 text-sm text-slate-200">
            {archivedCards.length} archived cards
          </div>
        </header>

        {error ? (
          <div className="mb-6 rounded-xl border border-rose-500/40 bg-rose-500/10 px-3 py-2 text-sm text-rose-200">
            {error}
          </div>
        ) : null}

        {loading ? (
          <div className="rounded-3xl border border-slate-800 bg-slate-900/80 p-8 text-slate-400">
            Loading archive...
          </div>
        ) : archivedCards.length === 0 ? (
          <div className="rounded-3xl border border-dashed border-slate-700 bg-slate-900/60 p-8 text-slate-400">
            Your archive is empty. Archived cards will appear here until you restore or permanently delete them.
          </div>
        ) : (
          <div className="space-y-4">
            {archivedCards.map((card) => (
              <div key={card.id} className="rounded-3xl border border-slate-800 bg-slate-900/80 p-5">
                <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                  <div>
                    <p className="text-xs uppercase tracking-[0.2em] text-cyan-300">Archived card</p>
                    <h2 className="mt-2 text-xl font-semibold text-white">{card.frontContent}</h2>
                    <p className="mt-2 text-slate-400">{card.backContent}</p>
                  </div>

                  <div className="flex flex-wrap items-center gap-3">
                    <select
                      value={restoreTargets[card.id] ?? ""}
                      onChange={(event) =>
                        setRestoreTargets((current) => ({ ...current, [card.id]: event.target.value }))
                      }
                      className="rounded-xl border border-slate-700 bg-slate-950 px-3 py-2 text-slate-100 outline-none focus:border-cyan-400"
                    >
                      <option value="">Choose a deck</option>
                      {decks.map((deck) => (
                        <option key={deck.id} value={String(deck.id)}>
                          {deck.title}
                        </option>
                      ))}
                    </select>

                    <button
                      type="button"
                      onClick={() => handleRestore(card.id)}
                      className="rounded-xl bg-cyan-400 px-3 py-2 font-medium text-slate-950 transition hover:bg-cyan-300"
                    >
                      Restore
                    </button>

                    <button
                      type="button"
                      onClick={() => handlePermanentDelete(card.id)}
                      className="rounded-xl border border-rose-500/50 bg-rose-500/10 px-3 py-2 font-medium text-rose-200 transition hover:bg-rose-500/20"
                    >
                      Delete permanently
                    </button>
                  </div>
                </div>

                {card.hints ? (
                  <div className="mt-4 rounded-xl border border-slate-700 bg-slate-950/60 px-3 py-2 text-sm text-slate-300">
                    Hint: {card.hints}
                  </div>
                ) : null}
              </div>
            ))}
          </div>
        )}
      </div>
    </main>
  );
}
