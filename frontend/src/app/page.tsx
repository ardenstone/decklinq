"use client";

import { useEffect, useState } from "react";

type Deck = {
  id: number;
  title: string;
  description: string;
  isPublic: boolean;
  cardCount: number;
};

type HealthStatus = {
  status: string;
  app: string;
  timestamp: string;
};

const apiBase = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

export default function Home() {
  const [health, setHealth] = useState<HealthStatus | null>(null);
  const [decks, setDecks] = useState<Deck[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadDashboard = async () => {
      try {
        const [healthResponse, decksResponse] = await Promise.all([
          fetch(`${apiBase}/api/health`),
          fetch(`${apiBase}/api/decks`),
        ]);

        if (!healthResponse.ok || !decksResponse.ok) {
          throw new Error("Unable to reach DeckLinq API");
        }

        const healthData = await healthResponse.json();
        const deckData = await decksResponse.json();

        setHealth(healthData);
        setDecks(deckData);
      } catch (error) {
        console.error(error);
        setHealth({ status: "offline", app: "DeckLinq", timestamp: new Date().toISOString() });
        setDecks([]);
      } finally {
        setLoading(false);
      }
    };

    loadDashboard();
  }, []);

  return (
    <main className="min-h-screen bg-[radial-gradient(circle_at_top,_#1e293b,_#020617_60%)] text-slate-100">
      <div className="mx-auto flex max-w-6xl flex-col gap-10 px-6 py-10 md:px-10">
        <header className="flex items-center justify-between border-b border-slate-700/80 pb-6">
          <div>
            <p className="text-sm uppercase tracking-[0.28em] text-cyan-300">DeckLinq</p>
            <h1 className="mt-2 text-3xl font-bold md:text-4xl">Learn faster with spaced repetition</h1>
          </div>
          <div className="rounded-full border border-cyan-400/40 bg-cyan-400/10 px-4 py-2 text-sm font-medium text-cyan-200">
            {loading ? "Checking backend..." : health?.status === "ok" ? "API online" : "API offline"}
          </div>
        </header>

        <section className="grid gap-6 md:grid-cols-3">
          <div className="rounded-2xl border border-slate-700 bg-slate-900/70 p-5 shadow-lg shadow-slate-950/30">
            <p className="text-sm text-slate-400">Active decks</p>
            <p className="mt-3 text-3xl font-bold">{decks.length}</p>
          </div>
          <div className="rounded-2xl border border-slate-700 bg-slate-900/70 p-5 shadow-lg shadow-slate-950/30">
            <p className="text-sm text-slate-400">Cards reviewed</p>
            <p className="mt-3 text-3xl font-bold">{decks.reduce((sum, deck) => sum + deck.cardCount, 0)}</p>
          </div>
          <div className="rounded-2xl border border-slate-700 bg-slate-900/70 p-5 shadow-lg shadow-slate-950/30">
            <p className="text-sm text-slate-400">Next review</p>
            <p className="mt-3 text-3xl font-bold">Today</p>
          </div>
        </section>

        <section className="grid gap-8 lg:grid-cols-[1.2fr_0.8fr]">
          <div className="rounded-3xl border border-slate-700 bg-slate-900/70 p-6">
            <div className="flex items-center justify-between">
              <h2 className="text-xl font-semibold">Your study queue</h2>
              <button className="rounded-full bg-cyan-500 px-4 py-2 text-sm font-semibold text-slate-950 transition hover:bg-cyan-400">
                Start session
              </button>
            </div>

            <div className="mt-6 space-y-4">
              {decks.length > 0 ? (
                decks.map((deck) => (
                  <div key={deck.id} className="flex items-center justify-between rounded-2xl border border-slate-700 bg-slate-800/80 p-4">
                    <div>
                      <h3 className="font-semibold text-white">{deck.title}</h3>
                      <p className="mt-1 text-sm text-slate-400">{deck.description || "A focused review deck."}</p>
                    </div>
                    <div className="text-right text-sm text-slate-300">
                      <p>{deck.cardCount} cards</p>
                      <p className="mt-1 text-cyan-300">{deck.isPublic ? "Public" : "Private"}</p>
                    </div>
                  </div>
                ))
              ) : (
                <div className="rounded-2xl border border-dashed border-slate-600 bg-slate-800/60 p-8 text-center text-slate-300">
                  {loading ? "Loading decks..." : "No decks are available yet. Start by creating one in the backend."}
                </div>
              )}
            </div>
          </div>

          <aside className="rounded-3xl border border-slate-700 bg-slate-900/70 p-6">
            <h2 className="text-xl font-semibold">Backend status</h2>
            <div className="mt-5 rounded-2xl border border-slate-700 bg-slate-800/80 p-4">
              <p className="text-sm text-slate-400">Service</p>
              <p className="mt-1 text-lg font-semibold text-cyan-300">{health?.app ?? "DeckLinq"}</p>
              <p className="mt-4 text-sm text-slate-400">Status</p>
              <p className="mt-1 text-lg font-semibold text-emerald-300">{health?.status ?? "checking..."}</p>
              <p className="mt-4 text-xs text-slate-500">
                Updated: {health ? new Date(health.timestamp).toLocaleString() : "Waiting..."}
              </p>
            </div>
          </aside>
        </section>
      </div>
    </main>
  );
}
