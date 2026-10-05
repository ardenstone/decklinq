"use client";

import { useRouter } from "next/navigation";
import { FormEvent, useEffect, useState } from "react";
import { apiFetch, clearStoredToken, type AuthUser, type Deck } from "@/lib/api";

const emptyDeckForm = {
  title: "",
  description: "",
  isPublic: false,
};

export default function DashboardPage() {
  const router = useRouter();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [decks, setDecks] = useState<Deck[]>([]);
  const [loading, setLoading] = useState(true);
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState(emptyDeckForm);

  const loadDashboard = async () => {
    try {
      const [userData, deckData] = await Promise.all([
        apiFetch<AuthUser>("/api/users/me"),
        apiFetch<Deck[]>("/api/decks"),
      ]);

      setUser(userData);
      setDecks(deckData);
    } catch (loadError) {
      if (loadError instanceof Error && loadError.message === "Unauthorized") {
        clearStoredToken();
        router.replace("/login");
        return;
      }

      setError(loadError instanceof Error ? loadError.message : "Unable to load your dashboard.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    let active = true;

    const load = async () => {
      try {
        const [userData, deckData] = await Promise.all([
          apiFetch<AuthUser>("/api/users/me"),
          apiFetch<Deck[]>("/api/decks"),
        ]);

        if (!active) {
          return;
        }

        setUser(userData);
        setDecks(deckData);
      } catch (loadError) {
        if (!active) {
          return;
        }

        if (loadError instanceof Error && loadError.message === "Unauthorized") {
          clearStoredToken();
          router.replace("/login");
          return;
        }

        setError(loadError instanceof Error ? loadError.message : "Unable to load your dashboard.");
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    };

    void load();

    return () => {
      active = false;
    };
  }, [router]);

  const handleCreateDeck = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setCreating(true);

    try {
      await apiFetch<Deck>("/api/decks", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(form),
      });

      setForm(emptyDeckForm);
      await loadDashboard();
    } catch (createError) {
      setError(createError instanceof Error ? createError.message : "Unable to create deck.");
    } finally {
      setCreating(false);
    }
  };

  const handleLogout = () => {
    clearStoredToken();
    router.push("/login");
  };

  return (
    <main className="min-h-screen bg-slate-950 px-6 py-8 text-slate-100">
      <div className="mx-auto max-w-6xl">
        <header className="mb-8 flex flex-col gap-4 rounded-3xl border border-slate-800 bg-slate-900/80 p-6 md:flex-row md:items-center md:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.3em] text-cyan-300">DeckLinq</p>
            <h1 className="mt-2 text-3xl font-bold">Study dashboard</h1>
          </div>
          <div className="flex items-center gap-3">
            <div className="rounded-full border border-cyan-500/40 bg-cyan-500/10 px-3 py-1.5 text-sm text-cyan-200">
              {user ? `Signed in as ${user.username}` : "Loading..."}
            </div>
            <button
              type="button"
              onClick={handleLogout}
              className="rounded-full border border-slate-600 px-3 py-1.5 text-sm text-slate-200 transition hover:border-slate-500 hover:bg-slate-800"
            >
              Log out
            </button>
          </div>
        </header>

        <div className="grid gap-8 lg:grid-cols-[0.92fr_1.08fr]">
          <section className="rounded-3xl border border-slate-800 bg-slate-900/70 p-6">
            <h2 className="text-xl font-semibold">Create a new deck</h2>
            <form className="mt-6 space-y-4" onSubmit={handleCreateDeck}>
              <div>
                <label className="mb-2 block text-sm text-slate-300" htmlFor="title">
                  Deck title
                </label>
                <input
                  id="title"
                  value={form.title}
                  onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))}
                  className="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-slate-100 outline-none focus:border-cyan-400"
                  placeholder="Spanish verbs"
                />
              </div>

              <div>
                <label className="mb-2 block text-sm text-slate-300" htmlFor="description">
                  Description
                </label>
                <textarea
                  id="description"
                  value={form.description}
                  onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
                  rows={4}
                  className="w-full rounded-xl border border-slate-700 bg-slate-950 px-3 py-2.5 text-slate-100 outline-none focus:border-cyan-400"
                  placeholder="What should this deck help you remember?"
                />
              </div>

              <label className="flex items-center gap-3 text-sm text-slate-300">
                <input
                  type="checkbox"
                  checked={form.isPublic}
                  onChange={(event) => setForm((current) => ({ ...current, isPublic: event.target.checked }))}
                  className="h-4 w-4 rounded border-slate-700 bg-slate-950"
                />
                Make this deck public
              </label>

              {error ? (
                <div className="rounded-xl border border-rose-500/40 bg-rose-500/10 px-3 py-2 text-sm text-rose-200">
                  {error}
                </div>
              ) : null}

              <button
                type="submit"
                disabled={creating || !form.title.trim()}
                className="w-full rounded-xl bg-cyan-400 px-4 py-3 font-semibold text-slate-950 transition hover:bg-cyan-300 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {creating ? "Creating deck..." : "Create deck"}
              </button>
            </form>
          </section>

          <section className="rounded-3xl border border-slate-800 bg-slate-900/70 p-6">
            <div className="flex items-center justify-between gap-3">
              <h2 className="text-xl font-semibold">Your decks</h2>
              <button
                type="button"
                onClick={() => router.push("/archive")}
                className="rounded-xl border border-slate-700 bg-slate-950 px-3 py-2 text-sm font-medium text-slate-200 transition hover:border-cyan-500/60 hover:bg-slate-900"
              >
                Archive
              </button>
            </div>
            <div className="mt-3 flex items-center justify-between">
              <span className="rounded-full border border-slate-700 bg-slate-800 px-3 py-1 text-sm text-slate-300">
                {decks.length} total
              </span>
            </div>

            {loading ? (
              <div className="mt-6 rounded-2xl border border-dashed border-slate-700 bg-slate-950/50 p-8 text-slate-400">
                Loading your deck library...
              </div>
            ) : decks.length === 0 ? (
              <div className="mt-6 rounded-2xl border border-dashed border-slate-700 bg-slate-950/50 p-8 text-slate-400">
                You do not have any decks yet. Create your first deck to begin studying.
              </div>
            ) : (
              <div className="mt-6 space-y-4">
                {decks.map((deck) => (
                  <div key={deck.id} className="rounded-2xl border border-slate-700 bg-slate-950/60 p-4 transition hover:border-cyan-500/60 hover:bg-slate-900">
                    <div className="flex items-start justify-between gap-4">
                      <div>
                        <h3 className="text-lg font-semibold text-white">{deck.title}</h3>
                        <p className="mt-1 text-sm text-slate-400">{deck.description || "No description added yet."}</p>
                      </div>
                      <span className="rounded-full border border-cyan-500/40 bg-cyan-500/10 px-2 py-1 text-xs font-medium text-cyan-200">
                        {deck.isPublic ? "Public" : "Private"}
                      </span>
                    </div>
                    <div className="mt-4 flex items-center justify-between gap-3 text-sm text-slate-300">
                      <span>{deck.cardCount} cards</span>
                      <span>Updated {new Date(deck.updatedAt).toLocaleDateString()}</span>
                    </div>
                    <div className="mt-4 flex flex-wrap gap-3">
                      <button
                        type="button"
                        onClick={() => router.push(`/decks/${deck.id}/study`)}
                        className="rounded-xl bg-cyan-400 px-3 py-2 font-medium text-slate-950 transition hover:bg-cyan-300"
                      >
                        Start deck
                      </button>
                      <button
                        type="button"
                        onClick={() => router.push(`/decks/${deck.id}`)}
                        className="rounded-xl border border-slate-600 bg-slate-900 px-3 py-2 font-medium text-slate-200 transition hover:border-slate-500 hover:bg-slate-800"
                      >
                        Edit
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </section>
        </div>
      </div>
    </main>
  );
}
